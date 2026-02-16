using System;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;
using EditorBridge.Editor.Handlers;
using EditorBridge.Editor.Models;
using EditorBridge.Editor.Settings;

namespace EditorBridge.Editor.Server
{
    /// <summary>
    /// The core HTTP server that exposes the Unity Editor to external tools via REST API.
    /// Marked with [InitializeOnLoad] so it is bootstrapped automatically when the Editor launches
    /// or scripts are recompiled (domain reload).
    /// </summary>
    [InitializeOnLoad]
    internal static class EditorBridgeServer
    {
        // The underlying .NET HttpListener that receives HTTP requests.
        private static HttpListener s_listener;

        // A dedicated background thread that runs the blocking listen loop.
        // Using a background thread (IsBackground = true) ensures it does not
        // prevent the Unity process from exiting.
        private static Thread s_listenerThread;

        // Used to signal graceful shutdown to the listen loop and any in-flight handlers.
        private static CancellationTokenSource s_cts;

        // Routes incoming HTTP requests to the appropriate handler based on method and path.
        private static RequestRouter s_router;

        /// <summary>
        /// Static constructor — invoked once by Unity thanks to [InitializeOnLoad].
        /// Registers a cleanup callback before assembly reload (domain reload) and
        /// optionally auto-starts the server based on user settings.
        /// </summary>
        static EditorBridgeServer()
        {
            // Stop the server before Unity reloads assemblies to avoid lingering listeners
            // that would conflict with the new domain.
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            if (EditorBridgeSettings.instance.AutoStart)
            {
                Start();
            }
        }

        /// <summary>
        /// Initializes and starts the HTTP server on the configured port.
        /// If the server is already running, this is a no-op.
        /// </summary>
        private static void Start()
        {
            // Guard: prevent double-start.
            if (s_listener != null)
            {
                return;
            }

            // Validate the configured port number.
            var port = EditorBridgeSettings.instance.Port;
            if (port is < 1 or > 65535)
            {
                Debug.LogError($"[EditorBridge] Invalid port: {port}. Must be between 1 and 65535.");
                return;
            }

            // Set up the request router and register all endpoint handlers.
            s_router = new RequestRouter();
            RegisterHandlers();

            // Create and start the HttpListener.
            // If binding fails (e.g. port already in use), clean up and abort.
            s_listener = new HttpListener();
            try
            {
                s_listener.Prefixes.Add($"http://localhost:{port}/");
                s_listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorBridge] Failed to start server on port {port}: {ex.Message}");

                try
                {
                    s_listener.Close();
                }
                catch
                {
                    // ignored
                }

                s_listener = null;
                s_router = null;
                return;
            }

            // Launch the background listen loop.
            // A CancellationTokenSource is created so that Stop() can signal the loop to exit.
            s_cts = new CancellationTokenSource();
            s_listenerThread = new Thread(ListenLoop) { IsBackground = true };
            s_listenerThread.Start();

            Debug.Log($"[EditorBridge] Server started on http://localhost:{port}/");
        }

        /// <summary>
        /// Gracefully shuts down the HTTP server.
        /// Called automatically before assembly reload, or can be called manually.
        /// </summary>
        private static void Stop()
        {
            if (s_listener == null) return;

            // Signal the listen loop and any in-flight request handlers to stop.
            s_cts?.Cancel();

            // Stop accepting new connections and release the underlying socket.
            try
            {
                s_listener.Stop();
                s_listener.Close();
            }
            catch
            {
                // ignore errors during shutdown
            }

            // Wait up to 1 second for the listen thread to exit cleanly.
            s_listenerThread?.Join(1000);

            // Release all references so the server can be restarted cleanly.
            s_listener = null;
            s_listenerThread = null;
            s_cts = null;
            s_router = null;
        }

        /// <summary>
        /// Registers all route handlers with the router.
        /// Add new handler registrations here as endpoints are implemented.
        /// </summary>
        private static void RegisterHandlers()
        {
            PingHandler.Register(s_router);
        }

        /// <summary>
        /// The main loop that runs on a background thread, continuously accepting
        /// incoming HTTP connections until cancellation is requested or the listener is stopped.
        /// </summary>
        private static void ListenLoop()
        {
            // Capture the CancellationTokenSource locally to avoid accessing a field
            // that may be set to null by Stop() on another thread.
            var cts = s_cts;
            if (cts == null)
            {
                return;
            }

            var token = cts.Token;

            // Cache the WaitHandle from the CancellationToken so we can use it
            // in WaitHandle.WaitAny below. This handle is signaled when Cancel() is called.
            var tokenWaitHandle = token.WaitHandle;

            // Keep accepting requests while the listener is active and no cancellation
            // has been requested.
            while (s_listener is { IsListening: true } && !token.IsCancellationRequested)
            {
                try
                {
                    // Begin an asynchronous wait for an incoming HTTP request.
                    // When a request arrives, ListenerCallbackAsync is invoked on a thread-pool thread.
                    var result = s_listener.BeginGetContext(ListenerCallbackAsync, s_listener);

                    // Wait for EITHER of two events:
                    //   1. result.AsyncWaitHandle — a new HTTP request has arrived
                    //   2. tokenWaitHandle        — cancellation has been requested (server shutting down)
                    // Without the tokenWaitHandle, this thread would block indefinitely waiting
                    // for a request and could not respond to a shutdown signal.
                    using var handle = result.AsyncWaitHandle;
                    WaitHandle.WaitAny(new[] { handle, tokenWaitHandle });
                }
                catch (ObjectDisposedException)
                {
                    // The listener was closed/disposed while waiting — exit the loop.
                    break;
                }
                catch (HttpListenerException)
                {
                    // The listener encountered a fatal error — exit the loop.
                    break;
                }
            }
        }

        /// <summary>
        /// Callback invoked on a thread-pool thread when an HTTP request arrives.
        /// Completes the async accept, routes the request, and writes the response.
        /// Uses async void because it is an <see cref="AsyncCallback"/> delegate; exceptions
        /// are caught and logged within the method.
        /// </summary>
        private static async void ListenerCallbackAsync(IAsyncResult ar)
        {
            try
            {
                var listener = (HttpListener)ar.AsyncState;
                HttpListenerContext context;

                // Complete the asynchronous accept started by BeginGetContext.
                // This may throw if the listener was stopped between Begin and End.
                try
                {
                    context = listener.EndGetContext(ar);
                }
                catch
                {
                    // The listener was stopped or disposed — nothing to handle.
                    return;
                }

                // Snapshot the router reference; it may be set to null by Stop().
                var router = s_router;
                if (router == null) return;

                // Obtain a cancellation token if the server is still running;
                // otherwise fall back to a non-cancellable token.
                var token = s_cts?.Token ?? CancellationToken.None;

                try
                {
                    // Delegate the request to the router, which matches it to a handler.
                    await router.HandleRequestAsync(context, token);
                }
                catch (Exception ex)
                {
                    // An unhandled exception escaped from a handler — log it and
                    // attempt to return a 500 Internal Server Error response.
                    Debug.LogError($"[EditorBridge] Unhandled exception: {ex}");
                    try
                    {
                        RequestRouter.WriteResponse(context, 500,
                            JsonUtility.ToJson(new ErrorResponse("Internal server error")));
                    }
                    catch
                    {
                        // ignore write errors
                    }
                }
            }
            catch (Exception e)
            {
                // Outermost safety net — ensures no exception goes unobserved
                // from this async void method.
                Debug.LogError($"[EditorBridge] Listener callback failed: {e}");
            }
        }
    }
}

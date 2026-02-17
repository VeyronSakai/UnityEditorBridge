using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
            RegisterHandlers(s_router);

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

            s_cts = new CancellationTokenSource();
            _ = ListenLoopAsync(s_cts.Token);

            Debug.Log($"[EditorBridge] Server started on http://localhost:{port}/");
        }

        /// <summary>
        /// Gracefully shuts down the HTTP server.
        /// Called automatically before assembly reload, or can be called manually.
        /// </summary>
        private static void Stop()
        {
            if (s_listener == null)
            {
                return;
            }

            s_cts?.Cancel();

            // Stop accepting new connections and release the underlying socket.
            // This also causes GetContextAsync to throw, ending the listen loop.
            try
            {
                s_listener.Stop();
                s_listener.Close();
            }
            catch
            {
                // ignore errors during shutdown
            }

            // Release all references so the server can be restarted cleanly.
            s_listener = null;
            s_cts = null;
            s_router = null;
        }

        /// <summary>
        /// Registers all route handlers with the router.
        /// Add new handler registrations here as endpoints are implemented.
        /// </summary>
        private static void RegisterHandlers(RequestRouter requestRouter)
        {
            PingHandler.Register(requestRouter);
        }

        /// <summary>
        /// Async listen loop that continuously accepts incoming HTTP connections
        /// until the listener is stopped.
        /// </summary>
        private static async Task ListenLoopAsync(CancellationToken token)
        {
            while (s_listener is { IsListening: true } && !token.IsCancellationRequested)
            {
                try
                {
                    var context = await s_listener.GetContextAsync();
                    _ = HandleContextAsync(context, token);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (HttpListenerException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Handles a single HTTP request: routes it to the appropriate handler and writes the response.
        /// </summary>
        private static async Task HandleContextAsync(HttpListenerContext context, CancellationToken token)
        {
            try
            {
                var router = s_router;
                if (router == null)
                {
                    return;
                }

                try
                {
                    await router.HandleRequestAsync(context, token);
                }
                catch (Exception ex)
                {
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
                Debug.LogError($"[EditorBridge] Request handling failed: {e}");
            }
        }
    }
}

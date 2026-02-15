using System;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;
using EditorBridge.Editor.Handlers;
using EditorBridge.Editor.Settings;

namespace EditorBridge.Editor.Server
{
    [InitializeOnLoad]
    internal static class EditorBridgeServer
    {
        static HttpListener _listener;
        static Thread _listenerThread;
        static CancellationTokenSource _cts;
        static RequestRouter _router;

        static EditorBridgeServer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            if (EditorBridgeSettings.instance.AutoStart)
            {
                Start();
            }
        }

        public static void Start()
        {
            if (_listener != null) return;

            var port = EditorBridgeSettings.instance.Port;
            if (port < 1 || port > 65535)
            {
                Debug.LogError($"[EditorBridge] Invalid port: {port}. Must be between 1 and 65535.");
                return;
            }

            _router = new RequestRouter();
            RegisterHandlers();

            _listener = new HttpListener();
            try
            {
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorBridge] Failed to start server on port {port}: {ex.Message}");
                try { _listener.Close(); } catch { }
                _listener = null;
                return;
            }

            _cts = new CancellationTokenSource();
            _listenerThread = new Thread(ListenLoop) { IsBackground = true };
            _listenerThread.Start();

            Debug.Log($"[EditorBridge] Server started on http://localhost:{port}/");
        }

        public static void Stop()
        {
            if (_listener == null) return;

            _cts?.Cancel();

            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch
            {
                // ignore errors during shutdown
            }

            _listenerThread?.Join(1000);
            _listener = null;
            _listenerThread = null;
            _cts = null;
            _router = null;
        }

        static void RegisterHandlers()
        {
            PingHandler.Register(_router);
        }

        static void ListenLoop()
        {
            var cts = _cts;
            if (cts == null) return;

            var token = cts.Token;
            var tokenWaitHandle = token.WaitHandle;

            while (_listener != null && _listener.IsListening && !token.IsCancellationRequested)
            {
                try
                {
                    var result = _listener.BeginGetContext(ListenerCallback, _listener);
                    using (var handle = result.AsyncWaitHandle)
                    {
                        WaitHandle.WaitAny(new[] { handle, tokenWaitHandle });
                    }
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

        static async void ListenerCallback(IAsyncResult ar)
        {
            var listener = (HttpListener)ar.AsyncState;
            HttpListenerContext context;

            try
            {
                context = listener.EndGetContext(ar);
            }
            catch
            {
                return;
            }

            try
            {
                await _router.HandleRequest(context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorBridge] Unhandled exception: {ex}");
                try
                {
                    RequestRouter.WriteResponse(context, 500, JsonHelper.Error("Internal server error"));
                }
                catch
                {
                    // ignore write errors
                }
            }
        }
    }
}

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
            _router = new RequestRouter();
            RegisterHandlers();

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");

            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorBridge] Failed to start server on port {port}: {ex.Message}");
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

            _listener = null;
            _listenerThread = null;
            _cts = null;
        }

        static void RegisterHandlers()
        {
            PingHandler.Register(_router);
        }

        static void ListenLoop()
        {
            while (_listener != null && _listener.IsListening && !_cts.IsCancellationRequested)
            {
                try
                {
                    var result = _listener.BeginGetContext(ListenerCallback, _listener);
                    WaitHandle.WaitAny(new[] { result.AsyncWaitHandle, _cts.Token.WaitHandle });
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
                try
                {
                    RequestRouter.WriteResponse(context, 500, JsonHelper.Error(ex.Message));
                }
                catch
                {
                    // ignore write errors
                }
            }
        }
    }
}

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UnityEngine;

namespace UniCortex.Editor.Infrastructures
{
    internal sealed class HttpListenerServer : IHttpServer
    {
        private readonly IRequestRouter _router;
        private readonly int _port;
        private HttpListener _listener;
        private CancellationTokenSource _cts;

        public HttpListenerServer(IRequestRouter router, int port)
        {
            _router = router;
            _port = port;
        }

        public void Start()
        {
            if (_listener != null)
            {
                return;
            }

            _listener = new HttpListener();
            try
            {
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UniCortex] Failed to start server on port {_port}: {ex.Message}");

                try
                {
                    _listener.Close();
                }
                catch
                {
                    // ignored
                }

                _listener = null;
                return;
            }

            _cts = new CancellationTokenSource();
            _ = ListenLoopAsync(_cts.Token);

            Debug.Log($"[UniCortex] Server started on http://localhost:{_port}/");
        }

        public void Stop()
        {
            if (_listener == null)
            {
                return;
            }

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
            _cts = null;
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            while (_listener is { IsListening: true } && !token.IsCancellationRequested)
            {
                try
                {
                    var httpContext = await _listener.GetContextAsync();
                    await HandleContextAsync(httpContext, token);
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

        private async Task HandleContextAsync(HttpListenerContext httpContext, CancellationToken token)
        {
            var context = new HttpListenerRequestContext(httpContext);
            try
            {
                try
                {
                    await _router.HandleRequestAsync(context, token);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UniCortex] Unhandled exception: {ex}");
                    try
                    {
                        await context.WriteResponseAsync(500,
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
                Debug.LogError($"[UniCortex] Request handling failed: {e}");
            }
        }
    }
}

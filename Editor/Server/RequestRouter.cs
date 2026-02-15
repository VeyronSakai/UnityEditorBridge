using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EditorBridge.Editor.Server
{
    internal class RequestRouter
    {
        readonly Dictionary<(string method, string path), Func<HttpListenerContext, Task>> _handlers =
            new Dictionary<(string method, string path), Func<HttpListenerContext, Task>>();

        readonly HashSet<string> _knownPaths = new HashSet<string>();

        public void Register(string method, string path, Func<HttpListenerContext, Task> handler)
        {
            _handlers[(method.ToUpperInvariant(), path)] = handler;
            _knownPaths.Add(path);
        }

        public async Task HandleRequest(HttpListenerContext context)
        {
            var method = context.Request.HttpMethod.ToUpperInvariant();
            var path = context.Request.Url.AbsolutePath.TrimEnd('/');
            if (string.IsNullOrEmpty(path)) path = "/";

            try
            {
                if (_handlers.TryGetValue((method, path), out var handler))
                {
                    await handler(context);
                }
                else if (_knownPaths.Contains(path))
                {
                    WriteResponse(context, 405, JsonHelper.Error("Method not allowed"));
                }
                else
                {
                    WriteResponse(context, 404, JsonHelper.Error("Not found"));
                }
            }
            catch (Exception ex)
            {
                WriteResponse(context, 500, JsonHelper.Error(ex.Message));
            }
        }

        public static void WriteResponse(HttpListenerContext context, int statusCode, string json)
        {
            var response = context.Response;
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}

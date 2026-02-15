using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EditorBridge.Editor.Models;
using UnityEngine;

namespace EditorBridge.Editor.Server
{
    /// <summary>
    /// A simple HTTP request router that dispatches incoming requests to registered handlers
    /// based on the HTTP method and URL path.
    ///
    /// Routing is exact-match only — no wildcards or path parameters.
    /// Paths are normalized (trailing slashes stripped) before matching.
    /// </summary>
    internal class RequestRouter
    {
        // Maps (HTTP method, normalized path) to an async handler function.
        // Example key: ("GET", "/ping")
        private readonly Dictionary<(string method, string path), Func<HttpListenerContext, CancellationToken, Task>>
            _handlers = new();

        // Tracks all registered paths regardless of method, so we can distinguish
        // "path exists but wrong method" (405) from "path does not exist at all" (404).
        private readonly HashSet<string> _knownPaths = new();

        /// <summary>
        /// Registers a handler for a specific HTTP method and path.
        /// If a handler already exists for the same method+path, it is replaced.
        /// </summary>
        /// <param name="method">HTTP method (e.g. "GET", "POST"). Case-insensitive.</param>
        /// <param name="path">The URL path (e.g. "/ping"). Trailing slashes are stripped.</param>
        /// <param name="handler">An async function that processes the request and writes the response.</param>
        public void Register(string method, string path, Func<HttpListenerContext, CancellationToken, Task> handler)
        {
            var normalized = NormalizePath(path);
            _handlers[(method.ToUpperInvariant(), normalized)] = handler;
            _knownPaths.Add(normalized);
        }

        /// <summary>
        /// Routes an incoming HTTP request to the matching handler.
        ///
        /// Resolution logic:
        ///   1. If a handler matches both the method and path → invoke it.
        ///   2. If the path is known but no handler exists for this method → 405 Method Not Allowed.
        ///   3. Otherwise → 404 Not Found.
        ///
        /// Any unhandled exception from a handler is caught, logged, and results in a 500 response.
        /// </summary>
        public async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var method = context.Request.HttpMethod.ToUpperInvariant();
            var path = NormalizePath(context.Request.Url.AbsolutePath);

            try
            {
                if (_handlers.TryGetValue((method, path), out var handler))
                {
                    // Exact match found — delegate to the handler.
                    await handler(context, cancellationToken);
                }
                else if (_knownPaths.Contains(path))
                {
                    // The path is registered but not for this HTTP method.
                    WriteResponse(context, 405, JsonUtility.ToJson(new ErrorResponse { error = "Method not allowed" }));
                }
                else
                {
                    // No handler registered for this path at all.
                    WriteResponse(context, 404, JsonUtility.ToJson(new ErrorResponse { error = "Not found" }));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorBridge] {method} {path} failed: {ex}");
                WriteResponse(context, 500, JsonUtility.ToJson(new ErrorResponse { error = "Internal server error" }));
            }
        }

        /// <summary>
        /// Writes a JSON response to the client with the given HTTP status code.
        /// This is a static helper so that both the router and individual handlers
        /// can use it to send responses.
        /// </summary>
        /// <param name="context">The HTTP listener context containing the response stream.</param>
        /// <param name="statusCode">The HTTP status code (e.g. 200, 404, 500).</param>
        /// <param name="json">The JSON string to write as the response body.</param>
        public static void WriteResponse(HttpListenerContext context, int statusCode, string json)
        {
            var response = context.Response;
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            try
            {
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch
            {
                // client may have disconnected during write
            }
            finally
            {
                // Always close the output stream to signal the end of the response,
                // even if the write itself failed.
                try
                {
                    response.OutputStream.Close();
                }
                catch
                {
                    // client may have already disconnected
                }
            }
        }

        /// <summary>
        /// Normalizes a URL path by stripping trailing slashes.
        /// The root path "/" is preserved as-is.
        /// Examples: "/ping/" => "/ping", "/" => "/", "" => "/"
        /// </summary>
        private static string NormalizePath(string path)
        {
            var trimmed = path.TrimEnd('/');
            return string.IsNullOrEmpty(trimmed) ? "/" : trimmed;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using EditorBridge.Editor.Models;
using UnityEngine;
using EditorBridge.Editor.Server;

namespace EditorBridge.Editor.Handlers
{
    /// <summary>
    /// Handler for the GET /ping endpoint.
    /// Used as a health-check to verify that the EditorBridge server is running
    /// and can successfully dispatch work to Unity's main thread.
    /// </summary>
    internal static class PingHandler
    {
        /// <summary>
        /// Registers the ping endpoint with the router.
        /// Called once during server startup from <see cref="EditorBridgeServer.RegisterHandlers"/>.
        /// </summary>
        public static void Register(RequestRouter router)
        {
            router.Register("GET", "/ping", HandlePingAsync);
        }

        /// <summary>
        /// Handles an incoming GET /ping request.
        /// Dispatches a Debug.Log("pong") call to the main thread (proving main-thread
        /// access works), then returns a 200 OK response with {"status":"ok"}.
        /// </summary>
        static async Task HandlePingAsync(System.Net.HttpListenerContext context, CancellationToken cancellationToken)
        {
            // Execute on the main thread to verify the dispatcher is functional.
            // Debug.Log is a Unity API that must be called from the main thread.
            await MainThreadDispatcher.RunOnMainThread(() => Debug.Log("pong"));

            var json = JsonUtility.ToJson(new PingResponse { status = "ok" });
            RequestRouter.WriteResponse(context, 200, json);
        }
    }
}

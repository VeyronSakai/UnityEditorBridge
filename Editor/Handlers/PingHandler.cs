using System.Threading.Tasks;
using UnityEngine;
using EditorBridge.Editor.Server;

namespace EditorBridge.Editor.Handlers
{
    internal static class PingHandler
    {
        public static void Register(RequestRouter router)
        {
            router.Register("GET", "/ping", HandlePing);
        }

        static async Task HandlePing(System.Net.HttpListenerContext context)
        {
            await MainThreadDispatcher.RunOnMainThread(() => Debug.Log("pong"));

            var json = JsonHelper.Object(("status", "ok"));
            RequestRouter.WriteResponse(context, 200, json);
        }
    }
}

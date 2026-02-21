using UniCortex.Editor.Infrastructures;
using UniCortex.Editor.Presentations;
using UniCortex.Editor.Settings;
using UniCortex.Editor.UseCases;
using UnityEditor;
using UnityEngine;

namespace UniCortex.Editor
{
    [InitializeOnLoad]
    internal static class EntryPoint
    {
        private static MainThreadDispatcher s_dispatcher;
        private static HttpListenerServer s_server;

        static EntryPoint()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;

            s_dispatcher = new MainThreadDispatcher();
            EditorApplication.update += s_dispatcher.OnUpdate;

            if (UniCortexSettings.instance.AutoStart)
            {
                StartServer();
            }
        }

        private static void StartServer()
        {
            var port = UniCortexSettings.instance.Port;
            if (port is < 1 or > 65535)
            {
                Debug.LogError($"[UniCortex] Invalid port: {port}. Must be between 1 and 65535.");
                return;
            }

            var pingUseCase = new PingUseCase(s_dispatcher);
            var pingHandler = new PingHandler(pingUseCase);

            var playUseCase = new PlayUseCase(s_dispatcher);
            var playHandler = new PlayHandler(playUseCase);

            var stopUseCase = new StopUseCase(s_dispatcher);
            var stopHandler = new StopHandler(stopUseCase);

            var getEditorStatusUseCase = new GetEditorStatusUseCase(s_dispatcher);
            var editorStatusHandler = new EditorStatusHandler(getEditorStatusUseCase);

            var router = new RequestRouter();
            pingHandler.Register(router);
            playHandler.Register(router);
            stopHandler.Register(router);
            editorStatusHandler.Register(router);

            s_server = new HttpListenerServer(router, port);
            s_server.Start();
        }

        private static void Shutdown()
        {
            s_server?.Stop();
            s_server = null;

            if (s_dispatcher != null)
            {
                EditorApplication.update -= s_dispatcher.OnUpdate;
                s_dispatcher = null;
            }
        }
    }
}

using UniCortex.Editor.Handlers.Editor;
using UniCortex.Editor.Infrastructures;
using UniCortex.Editor.Settings;
using UniCortex.Editor.UseCases;
using UnityEditor;

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
            EditorApplication.quitting += OnQuit;

            s_dispatcher = new MainThreadDispatcher();
            EditorApplication.update += s_dispatcher.OnUpdate;

            if (UniCortexSettings.instance.AutoStart)
            {
                StartServer();
            }
        }

        private static void StartServer()
        {
            var port = SessionState.GetInt("UniCortex.Port", 0);
            if (port == 0)
            {
                port = FindFreePort();
                SessionState.SetInt("UniCortex.Port", port);
            }

            var router = new RequestRouter();

            RegisterHandlers(router);

            s_server = new HttpListenerServer(router, port);
            s_server.Start();

            ServerUrlFile.Write(port);
        }

        private static void RegisterHandlers(RequestRouter router)
        {
            var editorApplication = new EditorApplicationAdapter();
            var compilationPipeline = new CompilationPipelineAdapter();

            var pingUseCase = new PingUseCase(s_dispatcher);
            var pingHandler = new PingHandler(pingUseCase);

            var playUseCase = new PlayUseCase(s_dispatcher, editorApplication);
            var playHandler = new PlayHandler(playUseCase);

            var stopUseCase = new StopUseCase(s_dispatcher, editorApplication);
            var stopHandler = new StopHandler(stopUseCase);

            var pauseUseCase = new PauseUseCase(s_dispatcher, editorApplication);
            var pauseHandler = new PauseHandler(pauseUseCase);

            var resumeUseCase = new ResumeUseCase(s_dispatcher, editorApplication);
            var resumeHandler = new ResumeHandler(resumeUseCase);

            var requestDomainReloadUseCase = new RequestDomainReloadUseCase(s_dispatcher, compilationPipeline);
            var requestDomainReloadHandler = new DomainReloadHandler(requestDomainReloadUseCase);

            var getEditorStatusUseCase = new GetEditorStatusUseCase(s_dispatcher, editorApplication);
            var editorStatusHandler = new EditorStatusHandler(getEditorStatusUseCase);

            pingHandler.Register(router);
            playHandler.Register(router);
            stopHandler.Register(router);
            pauseHandler.Register(router);
            resumeHandler.Register(router);
            requestDomainReloadHandler.Register(router);
            editorStatusHandler.Register(router);
        }

        private static int FindFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(
                System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static void OnQuit()
        {
            ServerUrlFile.Delete();
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

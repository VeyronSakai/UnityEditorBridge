using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;

namespace UniCortex.Editor.UseCases
{
    internal sealed class GetEditorStatusUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;
        private readonly IEditorApplication _editorApplication;

        public GetEditorStatusUseCase(IMainThreadDispatcher dispatcher, IEditorApplication editorApplication)
        {
            _dispatcher = dispatcher;
            _editorApplication = editorApplication;
        }

        public async Task<EditorStatusResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            var (isPlaying, isPaused) = await _dispatcher.RunOnMainThreadAsync(
                () => (_editorApplication.IsPlaying, _editorApplication.IsPaused), cancellationToken);
            return new EditorStatusResponse(isPlaying, isPaused);
        }
    }
}

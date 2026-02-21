using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UnityEditor;

namespace UniCortex.Editor.UseCases
{
    internal sealed class GetEditorStatusUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;

        public GetEditorStatusUseCase(IMainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<EditorStatusResponse> ExecuteAsync(CancellationToken cancellationToken)
        {
            var isPlaying = await _dispatcher.RunOnMainThreadAsync(() => EditorApplication.isPlaying, cancellationToken);
            return new EditorStatusResponse(isPlaying);
        }
    }
}

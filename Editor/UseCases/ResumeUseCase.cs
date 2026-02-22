using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UnityEngine;

namespace UniCortex.Editor.UseCases
{
    internal sealed class ResumeUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;
        private readonly IEditorApplication _editorApplication;

        public ResumeUseCase(IMainThreadDispatcher dispatcher, IEditorApplication editorApplication)
        {
            _dispatcher = dispatcher;
            _editorApplication = editorApplication;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dispatcher.RunOnMainThreadAsync(() =>
            {
                _editorApplication.IsPaused = false;
                Debug.Log("[UniCortex] Resume");
            }, cancellationToken);
        }
    }
}

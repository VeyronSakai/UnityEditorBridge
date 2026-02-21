using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UnityEditor;
using UnityEngine;

namespace UniCortex.Editor.UseCases
{
    internal sealed class UnpauseUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;

        public UnpauseUseCase(IMainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dispatcher.RunOnMainThreadAsync(() =>
            {
                EditorApplication.isPaused = false;
                Debug.Log("[UniCortex] Unpause");
            }, cancellationToken);
        }
    }
}

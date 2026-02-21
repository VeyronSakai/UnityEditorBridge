using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UnityEditor;
using UnityEngine;

namespace UniCortex.Editor.UseCases
{
    internal sealed class StopUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;

        public StopUseCase(IMainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dispatcher.RunOnMainThreadAsync(() =>
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[UniCortex] Stop");
            }, cancellationToken);
        }
    }
}

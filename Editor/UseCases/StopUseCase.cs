using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
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

        public async Task<PlayStopResponse> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dispatcher.RunOnMainThreadAsync(() =>
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[UniCortex] Stop");
            }, cancellationToken);
            return new PlayStopResponse(success: true);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UnityEditor;
using UnityEngine;

namespace UniCortex.Editor.UseCases
{
    internal sealed class PlayUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;

        public PlayUseCase(IMainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<PlayStopResponse> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dispatcher.RunOnMainThreadAsync(() =>
            {
                EditorApplication.isPlaying = true;
                Debug.Log("[UniCortex] Play");
            }, cancellationToken);
            return new PlayStopResponse(success: true);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UniCortex.Editor.UseCases;
using UnityEngine;

namespace UniCortex.Editor.Presentations
{
    internal sealed class PlayHandler
    {
        private readonly PlayUseCase _useCase;

        public PlayHandler(PlayUseCase useCase)
        {
            _useCase = useCase;
        }

        public void Register(IRequestRouter router)
        {
            router.Register(HttpMethodType.Post, ApiRoutes.Play, HandlePlayAsync);
        }

        private async Task HandlePlayAsync(IRequestContext context, CancellationToken cancellationToken)
        {
            await _useCase.ExecuteAsync(cancellationToken);
            var json = JsonUtility.ToJson(new PlayResponse(success: true));
            await context.WriteResponseAsync(200, json);
        }
    }
}

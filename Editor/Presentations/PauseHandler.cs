using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UniCortex.Editor.UseCases;
using UnityEngine;

namespace UniCortex.Editor.Presentations
{
    internal sealed class PauseHandler
    {
        private readonly PauseUseCase _useCase;

        public PauseHandler(PauseUseCase useCase)
        {
            _useCase = useCase;
        }

        public void Register(IRequestRouter router)
        {
            router.Register(HttpMethodType.Post, ApiRoutes.Pause, HandlePauseAsync);
        }

        private async Task HandlePauseAsync(IRequestContext context, CancellationToken cancellationToken)
        {
            await _useCase.ExecuteAsync(cancellationToken);
            var json = JsonUtility.ToJson(new PlayStopResponse(success: true));
            await context.WriteResponseAsync(200, json);
        }
    }
}

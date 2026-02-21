using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UniCortex.Editor.UseCases;
using UnityEngine;

namespace UniCortex.Editor.Presentations
{
    internal sealed class UnpauseHandler
    {
        private readonly UnpauseUseCase _useCase;

        public UnpauseHandler(UnpauseUseCase useCase)
        {
            _useCase = useCase;
        }

        public void Register(IRequestRouter router)
        {
            router.Register(HttpMethodType.Post, ApiRoutes.Unpause, HandleUnpauseAsync);
        }

        private async Task HandleUnpauseAsync(IRequestContext context, CancellationToken cancellationToken)
        {
            await _useCase.ExecuteAsync(cancellationToken);
            var json = JsonUtility.ToJson(new PlayStopResponse(success: true));
            await context.WriteResponseAsync(200, json);
        }
    }
}

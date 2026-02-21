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
            // Write the response before executing the use case.
            // Setting isPlaying = true triggers a domain reload, which can invalidate
            // the HTTP context before WriteResponseAsync is called.
            var json = JsonUtility.ToJson(new PlayStopResponse(success: true));
            await context.WriteResponseAsync(200, json);
            await _useCase.ExecuteAsync(cancellationToken);
        }
    }
}

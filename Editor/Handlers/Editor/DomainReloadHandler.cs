using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UniCortex.Editor.UseCases;
using UnityEngine;

namespace UniCortex.Editor.Handlers.Editor
{
    internal sealed class DomainReloadHandler
    {
        private readonly RequestDomainReloadUseCase _useCase;

        public DomainReloadHandler(RequestDomainReloadUseCase useCase)
        {
            _useCase = useCase;
        }

        public void Register(IRequestRouter router)
        {
            router.Register(HttpMethodType.Post, ApiRoutes.DomainReload, HandleDomainReloadAsync);
        }

        private async Task HandleDomainReloadAsync(IRequestContext context, CancellationToken cancellationToken)
        {
            // Write the response before executing the use case.
            // Domain reload invalidates the HTTP context before WriteResponseAsync can be called.
            var json = JsonUtility.ToJson(new DomainReloadResponse(success: true));
            await context.WriteResponseAsync(200, json);
            await _useCase.ExecuteAsync(cancellationToken);
        }
    }
}

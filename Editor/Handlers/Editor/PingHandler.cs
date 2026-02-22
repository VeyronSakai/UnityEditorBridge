using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UniCortex.Editor.UseCases;
using UnityEngine;

namespace UniCortex.Editor.Handlers.Editor
{
    internal sealed class PingHandler
    {
        private readonly PingUseCase _useCase;

        public PingHandler(PingUseCase useCase)
        {
            _useCase = useCase;
        }

        public void Register(IRequestRouter router)
        {
            router.Register(HttpMethodType.Get, ApiRoutes.Ping, HandlePingAsync);
        }

        private async Task HandlePingAsync(IRequestContext context, CancellationToken cancellationToken)
        {
            var message = await _useCase.ExecuteAsync(cancellationToken);
            var json = JsonUtility.ToJson(new PingResponse(status: "ok", message: message));
            await context.WriteResponseAsync(200, json);
        }
    }
}

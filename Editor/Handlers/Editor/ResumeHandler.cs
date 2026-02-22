using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UniCortex.Editor.Domains.Models;
using UniCortex.Editor.UseCases;
using UnityEngine;

namespace UniCortex.Editor.Handlers.Editor
{
    internal sealed class ResumeHandler
    {
        private readonly ResumeUseCase _useCase;

        public ResumeHandler(ResumeUseCase useCase)
        {
            _useCase = useCase;
        }

        public void Register(IRequestRouter router)
        {
            router.Register(HttpMethodType.Post, ApiRoutes.Resume, HandleResumeAsync);
        }

        private async Task HandleResumeAsync(IRequestContext context, CancellationToken cancellationToken)
        {
            await _useCase.ExecuteAsync(cancellationToken);
            var json = JsonUtility.ToJson(new ResumeResponse(success: true));
            await context.WriteResponseAsync(200, json);
        }
    }
}

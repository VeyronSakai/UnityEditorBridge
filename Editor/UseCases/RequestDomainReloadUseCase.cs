using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UnityEngine;

namespace UniCortex.Editor.UseCases
{
    internal sealed class RequestDomainReloadUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;
        private readonly ICompilationPipeline _compilationPipeline;

        public RequestDomainReloadUseCase(IMainThreadDispatcher dispatcher, ICompilationPipeline compilationPipeline)
        {
            _dispatcher = dispatcher;
            _compilationPipeline = compilationPipeline;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dispatcher.RunOnMainThreadAsync(() =>
            {
                Debug.Log("[UniCortex] Domain Reload");
                _compilationPipeline.RequestScriptCompilation();
            }, cancellationToken);
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniCortex.Editor.UseCases
{
    internal sealed class RequestDomainReloadUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;

        public RequestDomainReloadUseCase(IMainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _dispatcher.RunOnMainThreadAsync(() =>
            {
                Debug.Log("[UniCortex] Domain Reload");
                CompilationPipeline.RequestScriptCompilation();
            }, cancellationToken);
        }
    }
}

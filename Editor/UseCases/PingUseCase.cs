using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;
using UnityEngine;

namespace UniCortex.Editor.UseCases
{
    internal sealed class PingUseCase
    {
        private readonly IMainThreadDispatcher _dispatcher;

        public PingUseCase(IMainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
        {
            await _dispatcher.RunOnMainThreadAsync(() => Debug.Log("pong"), cancellationToken);
            return "pong";
        }
    }
}

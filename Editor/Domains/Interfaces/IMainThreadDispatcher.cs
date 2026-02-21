using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCortex.Editor.Domains.Interfaces
{
    internal interface IMainThreadDispatcher
    {
        Task<T> RunOnMainThreadAsync<T>(Func<T> func, CancellationToken cancellationToken = default);
        Task RunOnMainThreadAsync(Action action, CancellationToken cancellationToken = default);
    }
}

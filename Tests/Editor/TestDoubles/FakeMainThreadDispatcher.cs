using System;
using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;

namespace UniCortex.Editor.Tests.TestDoubles
{
    // IMPORTANT: All methods must return synchronously completed tasks
    // (Task.CompletedTask / Task.FromResult). Tests call .GetAwaiter().GetResult()
    // which blocks the thread. Under Unity's UnitySynchronizationContext, using
    // Task.Yield() or other truly asynchronous constructs would deadlock because
    // the continuation is posted back to the already-blocked thread.
    //
    // Async test methods ([Test] async Task) are not reliably supported until
    // Unity Test Framework 1.3+ (Unity 2023.1+). With Test Framework 1.1.x,
    // the synchronous-blocking + completed-task pattern used here is the
    // safest approach.
    internal sealed class FakeMainThreadDispatcher : IMainThreadDispatcher
    {
        public int CallCount { get; private set; }

        public Task<T> RunOnMainThreadAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<T>(cancellationToken);
            CallCount++;
            return Task.FromResult(func());
        }

        public Task RunOnMainThreadAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            CallCount++;
            action();
            return Task.CompletedTask;
        }
    }
}

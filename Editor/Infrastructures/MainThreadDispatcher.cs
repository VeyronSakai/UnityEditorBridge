using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;

namespace UniCortex.Editor.Infrastructures
{
    internal sealed class MainThreadDispatcher : IMainThreadDispatcher
    {
        private readonly ConcurrentQueue<Action> _queue = new();

        internal void OnUpdate()
        {
            while (_queue.TryDequeue(out var action))
            {
                action();
            }
        }

        public Task<T> RunOnMainThreadAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(cancellationToken);
            }

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            _queue.Enqueue(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        public Task RunOnMainThreadAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            _queue.Enqueue(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}

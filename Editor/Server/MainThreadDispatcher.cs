using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;

namespace EditorBridge.Editor.Server
{
    /// <summary>
    /// Provides a mechanism to schedule work onto Unity's main thread from background threads.
    ///
    /// Most Unity APIs are not thread-safe and must be called from the main thread.
    /// Because HTTP requests are handled on background / thread-pool threads, any handler
    /// that needs to interact with the Unity Editor (e.g. reading scene data, executing menu
    /// items) must marshal the call through this dispatcher.
    ///
    /// Implementation: a <see cref="ConcurrentQueue{T}"/> is drained every frame via
    /// <see cref="EditorApplication.update"/>, executing queued actions on the main thread.
    /// </summary>
    [InitializeOnLoad]
    internal static class MainThreadDispatcher
    {
        // A thread-safe queue that holds actions submitted from any thread.
        // Drained on the main thread each editor frame.
        private static readonly ConcurrentQueue<Action> s_queue = new();

        /// <summary>
        /// Static constructor — registers the per-frame update callback and ensures
        /// cleanup before assembly reload to avoid stale delegates.
        /// </summary>
        static MainThreadDispatcher()
        {
            // Pump the queue once per editor frame.
            EditorApplication.update += Update;

            // Unregister before domain reload so the delegate does not survive into the
            // new domain (which would cause a MissingMethodException).
            AssemblyReloadEvents.beforeAssemblyReload += () => EditorApplication.update -= Update;
        }

        /// <summary>
        /// Called every editor frame on the main thread.
        /// Drains all pending actions from the queue and executes them synchronously.
        /// </summary>
        private static void Update()
        {
            while (s_queue.TryDequeue(out var action))
            {
                action();
            }
        }

        /// <summary>
        /// Schedules a function that returns a value to run on the main thread and returns
        /// a <see cref="Task{T}"/> that completes with the result.
        ///
        /// Callers on background threads can <c>await</c> the returned task to asynchronously
        /// wait for the main-thread execution to finish.
        /// </summary>
        /// <typeparam name="T">The return type of <paramref name="func"/>.</typeparam>
        /// <param name="func">The function to execute on the main thread.</param>
        /// <returns>A task that resolves to the function's return value, or faults if it throws.</returns>
        public static Task<T> RunOnMainThread<T>(Func<T> func)
        {
            // RunContinuationsAsynchronously prevents continuations from running inline
            // on the main thread when SetResult/SetException is called, which could cause
            // unexpected re-entrancy or block the editor frame.
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            s_queue.Enqueue(() =>
            {
                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Schedules a void action to run on the main thread and returns a <see cref="Task"/>
        /// that completes when the action has executed.
        /// </summary>
        /// <param name="action">The action to execute on the main thread.</param>
        /// <returns>A task that completes when the action finishes, or faults if it throws.</returns>
        public static Task RunOnMainThread(Action action)
        {
            // Uses TaskCompletionSource<bool> as a stand-in because there is no non-generic
            // TaskCompletionSource in .NET Standard 2.1 / .NET Framework 4.x used by Unity.
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            s_queue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}

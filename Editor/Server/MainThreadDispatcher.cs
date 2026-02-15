using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;

namespace EditorBridge.Editor.Server
{
    [InitializeOnLoad]
    internal static class MainThreadDispatcher
    {
        static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        static MainThreadDispatcher()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            while (Queue.TryDequeue(out var action))
            {
                action();
            }
        }

        public static Task<T> RunOnMainThread<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Queue.Enqueue(() =>
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

        public static Task RunOnMainThread(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            Queue.Enqueue(() =>
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

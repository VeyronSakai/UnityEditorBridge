using System.Threading.Tasks;
using UniCortex.Editor.Domains.Interfaces;

namespace UniCortex.Editor.Tests.TestDoubles
{
    // IMPORTANT: Async methods must return synchronously completed tasks
    // (Task.CompletedTask / Task.FromResult). Do NOT use Task.Yield() or any
    // truly asynchronous construct here. Tests call .GetAwaiter().GetResult()
    // which blocks the thread. Under Unity's UnitySynchronizationContext the
    // continuation from Task.Yield() would be posted back to the blocked
    // thread, causing a deadlock.
    //
    // Async test methods ([Test] async Task) are not reliably supported until
    // Unity Test Framework 1.3+ (Unity 2023.1+). With Test Framework 1.1.x,
    // the synchronous-blocking + completed-task pattern used here is the
    // safest approach.
    internal sealed class FakeRequestContext : IRequestContext
    {
        public string HttpMethod { get; set; } = "GET";
        public string Path { get; set; } = "/";
        public string Body { get; set; } = "";

        public int ResponseStatusCode { get; private set; }
        public string ResponseBody { get; private set; }

        public Task<string> ReadBodyAsync()
        {
            return Task.FromResult(Body);
        }

        public Task WriteResponseAsync(int statusCode, string json)
        {
            ResponseStatusCode = statusCode;
            ResponseBody = json;
            return Task.CompletedTask;
        }
    }
}

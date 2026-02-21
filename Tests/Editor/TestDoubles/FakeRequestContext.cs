using System.Threading.Tasks;
using EditorBridge.Editor.Domains.Interfaces;

namespace EditorBridge.Editor.Tests.TestDoubles
{
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

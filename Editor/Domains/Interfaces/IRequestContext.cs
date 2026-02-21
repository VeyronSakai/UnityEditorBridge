using System.Threading.Tasks;

namespace UniCortex.Editor.Domains.Interfaces
{
    internal interface IRequestContext
    {
        string HttpMethod { get; }
        string Path { get; }
        Task<string> ReadBodyAsync();
        Task WriteResponseAsync(int statusCode, string json);
    }
}

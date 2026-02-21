using System.ComponentModel;
using System.Text.Json;
using UniCortex.Editor.Domains.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace UniCortex.Mcp.Tools;

[McpServerToolType, UsedImplicitly]
public class PingTool(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UniCortex");

    [McpServerTool(ReadOnly = true), Description("Check connectivity with the Unity Editor."), UsedImplicitly]
    public async Task<string> Ping(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(ApiRoutes.Ping, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var ping = JsonSerializer.Deserialize<PingResponse>(json, new JsonSerializerOptions { IncludeFields = true })!;
        return ping.message;
    }
}

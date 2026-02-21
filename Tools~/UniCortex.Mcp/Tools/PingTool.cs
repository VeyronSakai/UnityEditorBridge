using System.ComponentModel;
using System.Text.Json;
using UniCortex.Editor.Domains.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace UniCortex.Mcp.Tools;

[McpServerToolType, UsedImplicitly]
public class PingTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(ReadOnly = true), Description("Check connectivity with the Unity Editor."), UsedImplicitly]
    public async Task<string> Ping(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("UniCortex");
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };

        var response = await httpClient.GetAsync(ApiRoutes.Ping, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var ping = JsonSerializer.Deserialize<PingResponse>(json, jsonOptions)!;
        return ping.message;
    }
}

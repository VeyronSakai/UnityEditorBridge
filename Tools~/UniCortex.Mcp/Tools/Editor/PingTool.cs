using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class PingTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "editor_ping", ReadOnly = true), Description("Check connectivity with the Unity Editor."),
     UsedImplicitly]
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

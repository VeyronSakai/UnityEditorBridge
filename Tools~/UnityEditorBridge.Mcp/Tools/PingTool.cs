using System.ComponentModel;
using System.Text.Json;
using EditorBridge.Editor.Domains.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace UnityEditorBridge.Mcp.Tools;

[McpServerToolType, UsedImplicitly]
public class PingTool(IHttpClientFactory httpClientFactory)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { IncludeFields = true };
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("EditorBridge");

    [McpServerTool(ReadOnly = true), Description("Check connectivity with the Unity Editor."), UsedImplicitly]
    public async Task<string> Ping(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(ApiRoutes.Ping, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var ping = JsonSerializer.Deserialize<PingResponse>(json, s_jsonOptions)!;
        return ping.message;
    }
}

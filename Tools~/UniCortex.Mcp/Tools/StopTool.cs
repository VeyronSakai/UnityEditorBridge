using System.ComponentModel;
using System.Text.Json;
using UniCortex.Editor.Domains.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace UniCortex.Mcp.Tools;

[McpServerToolType, UsedImplicitly]
public class StopTool(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UniCortex");

    [McpServerTool(ReadOnly = false), Description("Stop Play Mode in the Unity Editor."), UsedImplicitly]
    public async Task<string> Stop(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(ApiRoutes.Stop, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<PlayStopResponse>(json, new JsonSerializerOptions { IncludeFields = true })!;
        return result.success.ToString();
    }
}

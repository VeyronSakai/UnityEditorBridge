using System.ComponentModel;
using System.Text.Json;
using UniCortex.Editor.Domains.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace UniCortex.Mcp.Tools;

[McpServerToolType, UsedImplicitly]
public class PlayTool(IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("UniCortex");

    [McpServerTool(ReadOnly = false), Description("Start Play Mode in the Unity Editor."), UsedImplicitly]
    public async Task<string> Play(CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(ApiRoutes.Play, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<PlayStopResponse>(json, new JsonSerializerOptions { IncludeFields = true })!;
        return result.success.ToString();
    }
}

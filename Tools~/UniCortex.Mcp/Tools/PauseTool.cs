using System.ComponentModel;
using System.Text.Json;
using UniCortex.Editor.Domains.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace UniCortex.Mcp.Tools;

[McpServerToolType, UsedImplicitly]
public class PauseTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(ReadOnly = false), Description("Pause Play Mode in the Unity Editor."), UsedImplicitly]
    public async Task<string> Pause(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("UniCortex");
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };

        var response = await httpClient.PostAsync(ApiRoutes.Pause, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        while (true)
        {
            var statusResponse = await httpClient.GetAsync(ApiRoutes.Status, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();
            var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(statusJson))
            {
                continue;
            }

            var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, jsonOptions)!;
            if (status.isPaused)
            {
                return "Play mode paused successfully.";
            }
        }
    }
}

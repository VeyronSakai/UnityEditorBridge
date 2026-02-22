using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class PauseTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "editor_pause", ReadOnly = false), Description("Pause the Unity Editor."), UsedImplicitly]
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
            var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, jsonOptions)!;
            if (status.isPaused)
            {
                return "Paused successfully.";
            }
        }
    }
}

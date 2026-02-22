using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class UnpauseTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "editor_unpause", ReadOnly = false), Description("Unpause the Unity Editor."), UsedImplicitly]
    public async Task<string> Unpause(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("UniCortex");
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
        var response = await httpClient.PostAsync(ApiRoutes.Unpause, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        while (true)
        {
            var statusResponse = await httpClient.GetAsync(ApiRoutes.Status, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();
            var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, jsonOptions)!;
            if (!status.isPaused)
            {
                return "Unpaused successfully.";
            }
        }
    }
}

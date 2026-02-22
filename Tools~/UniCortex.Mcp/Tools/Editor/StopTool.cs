using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class StopTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "editor_stop", ReadOnly = false), Description("Stop Play Mode in the Unity Editor."),
     UsedImplicitly]
    public async Task<string> Stop(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("UniCortex");
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };

        var response = await httpClient.PostAsync(ApiRoutes.Stop, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        while (true)
        {
            var statusResponse = await httpClient.GetAsync(ApiRoutes.Status, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();
            var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, jsonOptions)!;
            if (!status.isPlaying)
            {
                return "Play mode stopped successfully.";
            }
        }
    }
}

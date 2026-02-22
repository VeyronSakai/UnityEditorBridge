using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;
using UniCortex.Mcp.UseCases;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class PlayTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "editor_play", ReadOnly = false), Description("Start Play Mode in the Unity Editor."), UsedImplicitly]
    public async Task<string> Play(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("UniCortex");
        var jsonOptions = new JsonSerializerOptions { IncludeFields = true };

        await DomainReloadUseCase.ReloadAsync(httpClient, cancellationToken);

        var response = await httpClient.PostAsync(ApiRoutes.Play, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        while (true)
        {
            var statusResponse = await httpClient.GetAsync(ApiRoutes.Status, cancellationToken);
            statusResponse.EnsureSuccessStatusCode();
            var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
            var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, jsonOptions)!;
            if (status.isPlaying)
            {
                return "Play mode started successfully.";
            }
        }
    }
}

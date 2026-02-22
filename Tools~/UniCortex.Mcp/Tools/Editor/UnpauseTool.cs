using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;
using UniCortex.Mcp.Domains.Interfaces;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class UnpauseTool(IHttpClientFactory httpClientFactory, IUnityServerUrlProvider urlProvider)
{
    [McpServerTool(Name = "editor_unpause", ReadOnly = false), Description("Unpause the Unity Editor."), UsedImplicitly]
    public async Task<CallToolResult> Unpause(CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("UniCortex");
            var baseUrl = urlProvider.GetUrl();
            var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
            var response = await httpClient.PostAsync(baseUrl + ApiRoutes.Unpause, null, cancellationToken);
            response.EnsureSuccessStatusCode();

            while (true)
            {
                var statusResponse = await httpClient.GetAsync(baseUrl + ApiRoutes.Status, cancellationToken);
                statusResponse.EnsureSuccessStatusCode();
                var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                var status = JsonSerializer.Deserialize<EditorStatusResponse>(statusJson, jsonOptions)!;
                if (!status.isPaused)
                {
                    return new CallToolResult { Content = [new TextContentBlock { Text = "Unpaused successfully." }] };
                }
            }
        }
        catch (Exception ex)
        {
            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = ex.ToString() }] };
        }
    }
}

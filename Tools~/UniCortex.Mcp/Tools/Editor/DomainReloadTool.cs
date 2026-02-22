using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using UniCortex.Editor.Domains.Models;

namespace UniCortex.Mcp.Tools.Editor;

[McpServerToolType, UsedImplicitly]
public class DomainReloadTool(IHttpClientFactory httpClientFactory)
{
    [McpServerTool(Name = "editor_domain_reload", ReadOnly = false),
     Description("Request a domain reload (script recompilation) in the Unity Editor."), UsedImplicitly]
    public async Task<string> DomainReload(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("UniCortex");

        var response = await httpClient.PostAsync(ApiRoutes.DomainReload, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Poll /ping to wait for the server to come back after domain reload.
        // DomainReloadRetryHandler handles retries during the reload.
        var pingResponse = await httpClient.GetAsync(ApiRoutes.Ping, cancellationToken);
        pingResponse.EnsureSuccessStatusCode();

        return "Domain reload completed successfully.";
    }
}

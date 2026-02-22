using UniCortex.Editor.Domains.Models;

namespace UniCortex.Mcp.UseCases;

internal static class DomainReloadUseCase
{
    internal static async Task ReloadAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync(ApiRoutes.DomainReload, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Poll /ping to wait for the server to come back after domain reload.
        // DomainReloadRetryHandler handles retries during the reload.
        var pingResponse = await httpClient.GetAsync(ApiRoutes.Ping, cancellationToken);
        pingResponse.EnsureSuccessStatusCode();
    }
}

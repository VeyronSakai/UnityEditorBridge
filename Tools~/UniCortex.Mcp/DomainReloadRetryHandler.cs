using Microsoft.Extensions.Logging;

namespace UniCortex.Mcp;

public class DomainReloadRetryHandler(ILogger<DomainReloadRetryHandler> logger) : DelegatingHandler
{
    private static readonly TimeSpan s_maxWait = TimeSpan.FromHours(1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var logged = false;

        while (true)
        {
            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (logged)
                {
                    logger.LogInformation("Unity Editor is ready.");
                }

                return response;
            }
            catch (HttpRequestException) when (DateTime.UtcNow - startTime < s_maxWait)
            {
                if (!logged)
                {
                    logger.LogInformation(
                        "Unity Editor is not responding. Waiting for domain reload to complete...");
                    logged = true;
                }
            }
        }
    }
}

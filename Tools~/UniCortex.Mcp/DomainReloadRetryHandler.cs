using Microsoft.Extensions.Logging;

namespace UniCortex.Mcp;

public class DomainReloadRetryHandler(ILogger<DomainReloadRetryHandler> logger) : DelegatingHandler
{
    private static readonly TimeSpan s_maxWait = TimeSpan.FromHours(1);
    private static readonly TimeSpan s_initialDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_maxDelay = TimeSpan.FromSeconds(30);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var elapsed = TimeSpan.Zero;
        var delay = s_initialDelay;
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
            catch (HttpRequestException) when (elapsed < s_maxWait)
            {
                if (!logged)
                {
                    logger.LogInformation(
                        "Unity Editor is not responding. Waiting for domain reload to complete...");
                    logged = true;
                }

                await Task.Delay(delay, cancellationToken);
                elapsed += delay;
                delay = delay * 2 > s_maxDelay ? s_maxDelay : delay * 2;
            }
        }
    }
}

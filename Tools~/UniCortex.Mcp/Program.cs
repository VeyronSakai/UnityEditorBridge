using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UniCortex.Mcp.Infrastructures;
using UniCortex.Mcp.UseCases;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddTransient<HttpRequestHandler>();
builder.Services.AddHttpClient("UniCortex", client =>
{
    var baseUrl = Environment.GetEnvironmentVariable("UNICORTEX_URL") ?? "http://localhost:56780";
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
    {
        Console.Error.WriteLine(
            $"Invalid UNICORTEX_URL environment variable value: '{baseUrl}'. Please set it to a valid absolute URL (for example, 'http://localhost:56780').");
        Environment.Exit(1);
        return;
    }

    client.BaseAddress = baseUri;
}).AddHttpMessageHandler<HttpRequestHandler>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

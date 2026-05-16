using Hub;
using Hub.Components;
using Hub.Proxy;
using ModelContextProtocol.AspNetCore;

var options = ProxyOptions.Parse(args);
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://127.0.0.1:{options.UiPort}");

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<ProxyRuntimeState>();
builder.Services.AddHostedService<FluxzyProxyHostedService>();

builder.Services.AddRazorComponents();
builder.Services
    .AddMcpServer()
    .WithHttpTransport(transportOptions => transportOptions.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", (ProxyOptions proxyOptions, ProxyRuntimeState state) => Results.Ok(new
{
    ui = $"http://127.0.0.1:{proxyOptions.UiPort}",
    mcp = $"http://127.0.0.1:{proxyOptions.UiPort}/mcp",
    proxy = state.ProxyEndpoint,
    ca = state.CertificateUrl,
    state.StartedAt
}));

app.MapMcp("/mcp");
app.MapRazorComponents<App>();

Console.WriteLine($"Control UI: http://127.0.0.1:{options.UiPort}");
Console.WriteLine($"MCP endpoint: http://127.0.0.1:{options.UiPort}/mcp");

await app.RunAsync();

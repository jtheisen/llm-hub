using System.Net;
using Fluxzy;
using Fluxzy.Rules.Actions;

namespace HttpProxy.Proxy;

internal sealed class FluxzyProxyHostedService(
    ProxyOptions options,
    ProxyRuntimeState state,
    ILogger<FluxzyProxyHostedService> logger) : IHostedService, IAsyncDisposable
{
    private global::Fluxzy.Proxy? _proxy;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var setting = FluxzySetting
            .CreateDefault(IPAddress.Loopback, options.ProxyPort)
            .SetAutoInstallCertificate(options.InstallCertificate);

        if (!String.IsNullOrWhiteSpace(options.AuthHeaderName)
            && !String.IsNullOrWhiteSpace(options.AuthHeaderValue))
        {
            setting
                .ConfigureRule()
                .WhenAny()
                .Do(new AddRequestHeaderAction(options.AuthHeaderName, options.AuthHeaderValue));
        }

        _proxy = new global::Fluxzy.Proxy(setting);
        var endpoints = _proxy.Run().ToArray();
        var proxyEndpoint = String.Join(", ", endpoints.Select(endpoint => $"{endpoint.Address}:{endpoint.Port}"));
        var certificateUrl = $"http://localhost:{options.ProxyPort}/ca";

        state.MarkStarted(proxyEndpoint, certificateUrl);
        logger.LogInformation("Fluxzy proxy listening on {ProxyEndpoint}", proxyEndpoint);
        logger.LogInformation("Fluxzy CA certificate endpoint: {CertificateUrl}", certificateUrl);

        if (!String.IsNullOrWhiteSpace(options.AuthHeaderName))
        {
            logger.LogInformation("Adding request header {HeaderName}", options.AuthHeaderName);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_proxy is not null)
        {
            await _proxy.DisposeAsync();
            _proxy = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_proxy is not null)
        {
            await _proxy.DisposeAsync();
        }
    }
}

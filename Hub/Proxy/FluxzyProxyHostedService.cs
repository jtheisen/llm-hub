using System.Net;
using System.Text.RegularExpressions;
using Fluxzy;
using Fluxzy.Rules;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

namespace Hub.Proxy;

internal sealed class FluxzyProxyHostedService(
    ProxyOptions options,
    ProxySettings proxySettings,
    ProxyRuntimeState state,
    ILogger<FluxzyProxyHostedService> logger) : IHostedService, IAsyncDisposable
{
    private global::Fluxzy.Proxy? _proxy;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var setting = FluxzySetting
            .CreateDefault(IPAddress.Loopback, options.ProxyPort)
            .SetAutoInstallCertificate(options.InstallCertificate);

        ConfigureHeaderInjections(setting);
        ConfigureHostAliases(setting);

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

        logger.LogInformation(
            "Configured {AliasCount} host aliases and {HeaderRuleCount} header injection rules",
            proxySettings.HostAliases.Count,
            proxySettings.HeaderInjections.Count);

        return Task.CompletedTask;
    }

    private void ConfigureHeaderInjections(FluxzySetting setting)
    {
        foreach (var headerInjection in proxySettings.HeaderInjections)
        {
            var filters = BuildFilters(headerInjection).ToArray();
            var actions = BuildHeaderActions(headerInjection).ToArray();

            setting
                .ConfigureRule()
                .WhenAll(filters)
                .Do(actions[0], actions[1..]);
        }
    }

    private void ConfigureHostAliases(FluxzySetting setting)
    {
        foreach (var hostAlias in proxySettings.HostAliases)
        {
            var alias = GetRequiredValue(hostAlias.Alias, "Host alias entries require an Alias value.");
            var target = NormalizeTargetOrigin(hostAlias.Target, alias);

            setting
                .ConfigureRule()
                .When(new HostFilter(alias))
                .Do(new ForwardAction(target));

            logger.LogInformation("Forwarding alias {AliasHost} to {TargetOrigin}", alias, target);
        }
    }

    private static IEnumerable<Filter> BuildFilters(HeaderInjectionOptions headerInjection)
    {
        var hasFilter = false;

        if (!String.IsNullOrWhiteSpace(headerInjection.Host))
        {
            hasFilter = true;
            yield return new HostFilter(headerInjection.Host.Trim());
        }

        if (!String.IsNullOrWhiteSpace(headerInjection.PathRegex))
        {
            hasFilter = true;
            _ = new Regex(headerInjection.PathRegex);
            yield return new PathFilter(headerInjection.PathRegex, StringSelectorOperation.Regex);
        }

        if (!hasFilter)
        {
            throw new InvalidOperationException("Header injection entries require Host, PathRegex, or both.");
        }
    }

    private static IEnumerable<Fluxzy.Rules.Action> BuildHeaderActions(HeaderInjectionOptions headerInjection)
    {
        var headers = GetHeaders(headerInjection).ToArray();

        if (headers.Length == 0)
        {
            throw new InvalidOperationException("Header injection entries require BearerToken, Authorization, or Headers.");
        }

        foreach (var (name, value) in headers)
        {
            yield return new UpdateRequestHeaderAction(name, value)
            {
                AddIfMissing = true
            };
        }
    }

    private static IEnumerable<KeyValuePair<String, String>> GetHeaders(HeaderInjectionOptions headerInjection)
    {
        var setsAuthorizationHeader = !String.IsNullOrWhiteSpace(headerInjection.BearerToken)
            || !String.IsNullOrWhiteSpace(headerInjection.Authorization);
        var hasAuthorizationInHeaders = headerInjection.Headers.Keys.Any(
            key => key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));

        if (!String.IsNullOrWhiteSpace(headerInjection.BearerToken)
            && !String.IsNullOrWhiteSpace(headerInjection.Authorization))
        {
            throw new InvalidOperationException("Use either BearerToken or Authorization, not both.");
        }

        if (setsAuthorizationHeader && hasAuthorizationInHeaders)
        {
            throw new InvalidOperationException("Authorization can be configured once per header injection entry.");
        }

        if (!String.IsNullOrWhiteSpace(headerInjection.BearerToken))
        {
            yield return new KeyValuePair<String, String>("Authorization", $"Bearer {headerInjection.BearerToken}");
        }
        else if (!String.IsNullOrWhiteSpace(headerInjection.Authorization))
        {
            yield return new KeyValuePair<String, String>("Authorization", headerInjection.Authorization);
        }

        foreach (var (name, value) in headerInjection.Headers)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Injected header names cannot be empty.");
            }

            if (value is null)
            {
                throw new InvalidOperationException($"Injected header {name} cannot have a null value.");
            }

            yield return new KeyValuePair<String, String>(name, value);
        }
    }

    private static String NormalizeTargetOrigin(String target, String alias)
    {
        var value = GetRequiredValue(target, $"Host alias {alias} requires a Target value.");
        var candidate = value.Contains("://", StringComparison.Ordinal) ? value : $"https://{value}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || String.IsNullOrWhiteSpace(uri.Host)
            || !uri.PathAndQuery.Equals("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Host alias {alias} target must be a host or absolute origin, for example api.example.com or https://api.example.com.");
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private static String GetRequiredValue(String value, String message)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
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

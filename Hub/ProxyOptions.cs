namespace Hub;

internal sealed record ProxyOptions(
    Int32 ProxyPort,
    Int32 UiPort,
    Boolean InstallCertificate,
    String? AuthHeaderName,
    String? AuthHeaderValue)
{
    private const Int32 DefaultProxyPort = 8080;
    private const Int32 DefaultUiPort = 8081;

    public static ProxyOptions Parse(String[] args)
    {
        var proxyPort = GetPort(args, "--proxy-port", "HTTP_PROXY_PORT", DefaultProxyPort)
            ?? GetPort(args, "--port", "PORT", DefaultProxyPort)
            ?? DefaultProxyPort;
        var uiPort = GetPort(args, "--ui-port", "HTTP_PROXY_UI_PORT", DefaultUiPort) ?? DefaultUiPort;
        var installCertificate = args.Any(arg => arg.Equals("--install-cert", StringComparison.OrdinalIgnoreCase));
        var authHeaderName = GetOption(args, "--auth-header-name")
            ?? Environment.GetEnvironmentVariable("HTTP_PROXY_AUTH_HEADER_NAME");
        var authHeaderValue = GetOption(args, "--auth-header-value")
            ?? Environment.GetEnvironmentVariable("HTTP_PROXY_AUTH_HEADER_VALUE");

        return new ProxyOptions(proxyPort, uiPort, installCertificate, authHeaderName, authHeaderValue);
    }

    private static Int32? GetPort(String[] args, String optionName, String envName, Int32 defaultPort)
    {
        var portOption = GetOption(args, optionName);
        if (Int32.TryParse(portOption, out var optionPort))
        {
            return optionPort;
        }

        if (optionName.Equals("--proxy-port", StringComparison.OrdinalIgnoreCase))
        {
            var positionalPort = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
            if (Int32.TryParse(positionalPort, out var argPort))
            {
                return argPort;
            }
        }

        var envPort = Environment.GetEnvironmentVariable(envName);
        return Int32.TryParse(envPort, out var configuredPort) ? configuredPort : defaultPort;
    }

    private static String? GetOption(String[] args, String name)
    {
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            var prefix = $"{name}=";

            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[prefix.Length..];
            }

            if (arg.Equals(name, StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }

        return null;
    }
}

internal sealed class ProxySettings
{
    public List<HostAliasOptions> HostAliases { get; init; } = [];

    public List<HeaderInjectionOptions> HeaderInjections { get; init; } = [];
}

internal sealed class HostAliasOptions
{
    public String Alias { get; init; } = "";

    public String Target { get; init; } = "";
}

internal sealed class HeaderInjectionOptions
{
    public String? Host { get; init; }

    public String? PathRegex { get; init; }

    public String? BearerToken { get; init; }

    public String? Authorization { get; init; }

    public Dictionary<String, String?> Headers { get; init; } = [];
}

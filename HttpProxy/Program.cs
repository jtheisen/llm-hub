using System.Net;
using Fluxzy;
using Fluxzy.Rules.Actions;

var options = ProxyOptions.Parse(args);
var setting = FluxzySetting
    .CreateDefault(IPAddress.Loopback, options.Port)
    .SetAutoInstallCertificate(options.InstallCertificate);

if (!String.IsNullOrWhiteSpace(options.AuthHeaderName)
    && !String.IsNullOrWhiteSpace(options.AuthHeaderValue))
{
    setting
        .ConfigureRule()
        .WhenAny()
        .Do(new AddRequestHeaderAction(options.AuthHeaderName, options.AuthHeaderValue));
}

await using var proxy = new Proxy(setting);
var endpoints = proxy.Run();

Console.WriteLine($"HttpProxy listening on {String.Join(", ", endpoints.Select(endpoint => $"{endpoint.Address}:{endpoint.Port}"))}");
Console.WriteLine("Configure your browser HTTP/HTTPS proxy to 127.0.0.1 with this port.");
Console.WriteLine($"Certificate: http://localhost:{options.Port}/ca");
Console.WriteLine("Pass --install-cert to let Fluxzy install its root certificate in the user store.");

if (!String.IsNullOrWhiteSpace(options.AuthHeaderName))
{
    Console.WriteLine($"Adding request header: {options.AuthHeaderName}");
}

Console.WriteLine("Press Ctrl+C to stop.");

var stopped = new TaskCompletionSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stopped.TrySetResult();
};

await stopped.Task;

internal sealed record ProxyOptions(
    Int32 Port,
    Boolean InstallCertificate,
    String? AuthHeaderName,
    String? AuthHeaderValue)
{
    private const Int32 DefaultPort = 8080;

    public static ProxyOptions Parse(String[] args)
    {
        var port = GetPort(args);
        var installCertificate = args.Any(arg => arg.Equals("--install-cert", StringComparison.OrdinalIgnoreCase));
        var authHeaderName = GetOption(args, "--auth-header-name")
            ?? Environment.GetEnvironmentVariable("HTTP_PROXY_AUTH_HEADER_NAME");
        var authHeaderValue = GetOption(args, "--auth-header-value")
            ?? Environment.GetEnvironmentVariable("HTTP_PROXY_AUTH_HEADER_VALUE");

        return new ProxyOptions(port, installCertificate, authHeaderName, authHeaderValue);
    }

    private static Int32 GetPort(String[] args)
    {
        var portOption = GetOption(args, "--port");
        if (Int32.TryParse(portOption, out var optionPort))
        {
            return optionPort;
        }

        var positionalPort = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
        if (Int32.TryParse(positionalPort, out var argPort))
        {
            return argPort;
        }

        var envPort = Environment.GetEnvironmentVariable("HTTP_PROXY_PORT");
        return Int32.TryParse(envPort, out var configuredPort) ? configuredPort : DefaultPort;
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

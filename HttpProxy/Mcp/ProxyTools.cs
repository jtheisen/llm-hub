using System.ComponentModel;
using HttpProxy.Proxy;
using ModelContextProtocol.Server;

namespace HttpProxy.Mcp;

[McpServerToolType]
internal sealed class ProxyTools(ProxyOptions options, ProxyRuntimeState state)
{
    [McpServerTool]
    [Description("Gets the local proxy, UI, MCP, and certificate endpoints.")]
    public String GetProxyStatus()
    {
        return String.Join(Environment.NewLine, [
            $"Proxy: {state.ProxyEndpoint}",
            $"Certificate: {state.CertificateUrl}",
            $"UI: http://127.0.0.1:{options.UiPort}",
            $"MCP: http://127.0.0.1:{options.UiPort}/mcp",
            $"Started: {state.StartedAt?.ToString("O") ?? "not started"}"
        ]);
    }
}

namespace HttpProxy.Proxy;

internal sealed class ProxyRuntimeState
{
    public DateTimeOffset? StartedAt { get; private set; }

    public String ProxyEndpoint { get; private set; } = "not started";

    public String CertificateUrl { get; private set; } = "not available";

    public void MarkStarted(String proxyEndpoint, String certificateUrl)
    {
        StartedAt = DateTimeOffset.Now;
        ProxyEndpoint = proxyEndpoint;
        CertificateUrl = certificateUrl;
    }
}

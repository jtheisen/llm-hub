namespace Hub;

internal sealed class HttpApiSettings
{
    public List<HttpApiEndpointOptions> Endpoints { get; init; } = [];
}

internal sealed class HttpApiEndpointOptions
{
    public String Name { get; init; } = "";

    public String BaseUrl { get; init; } = "";

    public String? BearerToken { get; init; }

    public String? BasicToken { get; init; }

    public String? Authorization { get; init; }

    public Dictionary<String, String?> Headers { get; init; } = [];

    public Dictionary<String, String?> Query { get; init; } = [];
}

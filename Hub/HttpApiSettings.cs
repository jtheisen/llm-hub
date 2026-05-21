namespace Hub;

internal sealed class HttpApiSettings
{
    public List<HttpApiEndpointOptions> Endpoints { get; init; } = [];
}

internal sealed class HttpApiEndpointOptions
{
    public String Name { get; init; } = "";

    public String BaseUrl { get; init; } = "";

    public String? Description { get; init; }

    public List<String> Examples { get; init; } = [];

    public String? BearerToken { get; init; }

    public String? BasicCredentials { get; init; }

    public String? Authorization { get; init; }

    public Dictionary<String, String?> Headers { get; init; } = [];

    public Dictionary<String, String?> Query { get; init; } = [];

    public Dictionary<String, HttpApiRequestTemplateOptions> Requests { get; init; } = [];
}

internal sealed class HttpApiRequestTemplateOptions
{
    public String Method { get; init; } = "GET";

    public String Path { get; init; } = "";

    public String? Description { get; init; }

    public String? ContentType { get; init; }

    public Dictionary<String, String?> Headers { get; init; } = [];

    public Dictionary<String, String?> Query { get; init; } = [];
}

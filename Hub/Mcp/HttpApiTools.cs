using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Hub.Mcp;

[ModelContextProtocol.Server.McpServerToolType]
internal sealed class HttpApiTools(HttpApiSettings settings, IHttpClientFactory httpClientFactory)
{
    private const Int32 DefaultMaxResponseBytes = 200_000;
    private const Int32 AbsoluteMaxResponseBytes = 5_000_000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Lists configured HTTP API endpoints, including base URLs for relative request paths, without exposing authentication secrets.")]
    public String ListConfiguredHttpApis()
    {
        var endpoints = settings.Endpoints
            .Select(endpoint =>
            {
                var resolvedEndpoint = ResolveEndpoint(endpoint);

                return new
                {
                    resolvedEndpoint.Name,
                    resolvedEndpoint.BaseUrl,
                    BearerTokenConfigured = !String.IsNullOrWhiteSpace(resolvedEndpoint.BearerToken),
                    BasicTokenConfigured = !String.IsNullOrWhiteSpace(resolvedEndpoint.BasicToken),
                    AuthorizationConfigured = !String.IsNullOrWhiteSpace(resolvedEndpoint.Authorization)
                        || resolvedEndpoint.Headers.Keys.Any(key => key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)),
                    Headers = resolvedEndpoint.Headers.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                    Query = resolvedEndpoint.Query.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray()
                };
            })
            .ToArray();

        return ToJson(new { endpoints });
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Sends a GET request to a configured HTTP API endpoint and returns JSON with status, headers, and a parsed response body when possible.")]
    public Task<String> HttpApiGet(
        [Description("The configured HTTP API endpoint name.")]
        String apiName,
        [Description("A relative path under the configured BaseUrl. The path may include a query string.")]
        String path,
        [Description("Optional query parameters. Array values are sent as repeated query parameters.")]
        Dictionary<String, JsonElement>? query = null,
        [Description("Optional request headers for this call. Authorization is normally configured on the endpoint.")]
        Dictionary<String, String?>? headers = null,
        [Description("Maximum response body bytes to return. Defaults to 200000 and is capped at 5000000.")]
        Int32 maxResponseBytes = DefaultMaxResponseBytes,
        [Description("Request timeout in seconds. Defaults to 100.")]
        Int32 timeoutSeconds = 100)
    {
        return SendAsync(HttpMethod.Get, apiName, path, query, null, null, headers, maxResponseBytes, timeoutSeconds);
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Sends a HEAD request to a configured HTTP API endpoint and returns JSON with status and headers.")]
    public Task<String> HttpApiHead(
        [Description("The configured HTTP API endpoint name.")]
        String apiName,
        [Description("A relative path under the configured BaseUrl. The path may include a query string.")]
        String path,
        [Description("Optional query parameters. Array values are sent as repeated query parameters.")]
        Dictionary<String, JsonElement>? query = null,
        [Description("Optional request headers for this call. Authorization is normally configured on the endpoint.")]
        Dictionary<String, String?>? headers = null,
        [Description("Maximum response body bytes to return if the server sends a body. Defaults to 200000 and is capped at 5000000.")]
        Int32 maxResponseBytes = DefaultMaxResponseBytes,
        [Description("Request timeout in seconds. Defaults to 100.")]
        Int32 timeoutSeconds = 100)
    {
        return SendAsync(HttpMethod.Head, apiName, path, query, null, null, headers, maxResponseBytes, timeoutSeconds);
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Sends an OPTIONS request to a configured HTTP API endpoint and returns JSON with status, headers, and a parsed response body when possible.")]
    public Task<String> HttpApiOptions(
        [Description("The configured HTTP API endpoint name.")]
        String apiName,
        [Description("A relative path under the configured BaseUrl. The path may include a query string.")]
        String path,
        [Description("Optional query parameters. Array values are sent as repeated query parameters.")]
        Dictionary<String, JsonElement>? query = null,
        [Description("Optional request headers for this call. Authorization is normally configured on the endpoint.")]
        Dictionary<String, String?>? headers = null,
        [Description("Maximum response body bytes to return. Defaults to 200000 and is capped at 5000000.")]
        Int32 maxResponseBytes = DefaultMaxResponseBytes,
        [Description("Request timeout in seconds. Defaults to 100.")]
        Int32 timeoutSeconds = 100)
    {
        return SendAsync(HttpMethod.Options, apiName, path, query, null, null, headers, maxResponseBytes, timeoutSeconds);
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Sends a POST request to a configured HTTP API endpoint and returns JSON with status, headers, and a parsed response body when possible.")]
    public Task<String> HttpApiPost(
        [Description("The configured HTTP API endpoint name.")]
        String apiName,
        [Description("A relative path under the configured BaseUrl. The path may include a query string.")]
        String path,
        [Description("Optional JSON request body. String bodies are sent as raw text when ContentType is not JSON.")]
        JsonElement? body = null,
        [Description("Optional request content type. Defaults to application/json when a body is provided.")]
        String? contentType = null,
        [Description("Optional query parameters. Array values are sent as repeated query parameters.")]
        Dictionary<String, JsonElement>? query = null,
        [Description("Optional request headers for this call. Authorization is normally configured on the endpoint.")]
        Dictionary<String, String?>? headers = null,
        [Description("Maximum response body bytes to return. Defaults to 200000 and is capped at 5000000.")]
        Int32 maxResponseBytes = DefaultMaxResponseBytes,
        [Description("Request timeout in seconds. Defaults to 100.")]
        Int32 timeoutSeconds = 100)
    {
        return SendAsync(HttpMethod.Post, apiName, path, query, body, contentType, headers, maxResponseBytes, timeoutSeconds);
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Sends a PUT request to a configured HTTP API endpoint and returns JSON with status, headers, and a parsed response body when possible.")]
    public Task<String> HttpApiPut(
        [Description("The configured HTTP API endpoint name.")]
        String apiName,
        [Description("A relative path under the configured BaseUrl. The path may include a query string.")]
        String path,
        [Description("Optional JSON request body. String bodies are sent as raw text when ContentType is not JSON.")]
        JsonElement? body = null,
        [Description("Optional request content type. Defaults to application/json when a body is provided.")]
        String? contentType = null,
        [Description("Optional query parameters. Array values are sent as repeated query parameters.")]
        Dictionary<String, JsonElement>? query = null,
        [Description("Optional request headers for this call. Authorization is normally configured on the endpoint.")]
        Dictionary<String, String?>? headers = null,
        [Description("Maximum response body bytes to return. Defaults to 200000 and is capped at 5000000.")]
        Int32 maxResponseBytes = DefaultMaxResponseBytes,
        [Description("Request timeout in seconds. Defaults to 100.")]
        Int32 timeoutSeconds = 100)
    {
        return SendAsync(HttpMethod.Put, apiName, path, query, body, contentType, headers, maxResponseBytes, timeoutSeconds);
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Sends a PATCH request to a configured HTTP API endpoint and returns JSON with status, headers, and a parsed response body when possible.")]
    public Task<String> HttpApiPatch(
        [Description("The configured HTTP API endpoint name.")]
        String apiName,
        [Description("A relative path under the configured BaseUrl. The path may include a query string.")]
        String path,
        [Description("Optional JSON request body. String bodies are sent as raw text when ContentType is not JSON.")]
        JsonElement? body = null,
        [Description("Optional request content type. Defaults to application/json when a body is provided.")]
        String? contentType = null,
        [Description("Optional query parameters. Array values are sent as repeated query parameters.")]
        Dictionary<String, JsonElement>? query = null,
        [Description("Optional request headers for this call. Authorization is normally configured on the endpoint.")]
        Dictionary<String, String?>? headers = null,
        [Description("Maximum response body bytes to return. Defaults to 200000 and is capped at 5000000.")]
        Int32 maxResponseBytes = DefaultMaxResponseBytes,
        [Description("Request timeout in seconds. Defaults to 100.")]
        Int32 timeoutSeconds = 100)
    {
        return SendAsync(HttpMethod.Patch, apiName, path, query, body, contentType, headers, maxResponseBytes, timeoutSeconds);
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Sends a DELETE request to a configured HTTP API endpoint and returns JSON with status, headers, and a parsed response body when possible.")]
    public Task<String> HttpApiDelete(
        [Description("The configured HTTP API endpoint name.")]
        String apiName,
        [Description("A relative path under the configured BaseUrl. The path may include a query string.")]
        String path,
        [Description("Optional JSON request body. String bodies are sent as raw text when ContentType is not JSON.")]
        JsonElement? body = null,
        [Description("Optional request content type. Defaults to application/json when a body is provided.")]
        String? contentType = null,
        [Description("Optional query parameters. Array values are sent as repeated query parameters.")]
        Dictionary<String, JsonElement>? query = null,
        [Description("Optional request headers for this call. Authorization is normally configured on the endpoint.")]
        Dictionary<String, String?>? headers = null,
        [Description("Maximum response body bytes to return. Defaults to 200000 and is capped at 5000000.")]
        Int32 maxResponseBytes = DefaultMaxResponseBytes,
        [Description("Request timeout in seconds. Defaults to 100.")]
        Int32 timeoutSeconds = 100)
    {
        return SendAsync(HttpMethod.Delete, apiName, path, query, body, contentType, headers, maxResponseBytes, timeoutSeconds);
    }

    private async Task<String> SendAsync(
        HttpMethod method,
        String apiName,
        String path,
        Dictionary<String, JsonElement>? query,
        JsonElement? body,
        String? contentType,
        Dictionary<String, String?>? headers,
        Int32 maxResponseBytes,
        Int32 timeoutSeconds)
    {
        var endpoint = GetEndpoint(apiName);
        var uri = BuildUri(endpoint, path, query);
        using var request = new HttpRequestMessage(method, uri);
        request.Content = CreateContent(body, contentType);

        AddAuthorization(request, endpoint);
        AddHeaders(request, endpoint.Headers);
        AddHeaders(request, headers);

        var client = httpClientFactory.CreateClient();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 1)));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
        var bodyResult = await ReadBodyAsync(response, maxResponseBytes, cancellationTokenSource.Token);

        return ToJson(new
        {
            api = endpoint.Name,
            method = method.Method,
            requestUri = uri.ToString(),
            statusCode = (Int32)response.StatusCode,
            reasonPhrase = response.ReasonPhrase,
            success = response.IsSuccessStatusCode,
            headers = GetResponseHeaders(response),
            body = bodyResult.Body,
            bodyResult.Truncated
        });
    }

    private HttpApiEndpointOptions GetEndpoint(String apiName)
    {
        var matches = settings.Endpoints
            .Where(endpoint => endpoint.Name.Equals(apiName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"No HTTP API endpoint named '{apiName}' is configured.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Multiple HTTP API endpoints named '{apiName}' are configured.");
        }

        return ResolveEndpoint(matches[0]);
    }

    private static HttpApiEndpointOptions ResolveEndpoint(HttpApiEndpointOptions endpoint)
    {
        var name = GetRequiredValue(endpoint.Name, "HTTP API endpoints require a Name value.");
        var baseUrl = GetRequiredValue(endpoint.BaseUrl, $"HTTP API endpoint {endpoint.Name} requires a BaseUrl value.");

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || !baseUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !baseUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"HTTP API endpoint {endpoint.Name} BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        ValidateAuthorization(endpoint);

        return new HttpApiEndpointOptions
        {
            Name = name,
            BaseUrl = baseUrl,
            BearerToken = endpoint.BearerToken,
            BasicToken = endpoint.BasicToken,
            Authorization = endpoint.Authorization,
            Headers = endpoint.Headers,
            Query = endpoint.Query
        };
    }

    private static void ValidateAuthorization(HttpApiEndpointOptions endpoint)
    {
        var configuredAuthorizationSources = new[]
        {
            endpoint.BearerToken,
            endpoint.BasicToken,
            endpoint.Authorization,
            endpoint.Headers.Keys.Any(key => key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)) ? "Authorization header" : null
        }.Count(value => !String.IsNullOrWhiteSpace(value));

        if (configuredAuthorizationSources > 1)
        {
            throw new InvalidOperationException(
                $"HTTP API endpoint {endpoint.Name} can configure authorization once: BearerToken, BasicToken, Authorization, or Headers.Authorization.");
        }
    }

    private static Uri BuildUri(HttpApiEndpointOptions endpoint, String path, Dictionary<String, JsonElement>? query)
    {
        var baseUri = new Uri(endpoint.BaseUrl, UriKind.Absolute);

        if (Uri.TryCreate(path, UriKind.Absolute, out _) || path.StartsWith("//", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("HTTP API request paths must be relative to the configured BaseUrl.");
        }

        var uri = new Uri(baseUri, path);
        var builder = new UriBuilder(uri);
        var queryParts = new List<String>();

        AddExistingQuery(queryParts, builder.Query);
        AddConfiguredQuery(queryParts, endpoint.Query);
        AddRuntimeQuery(queryParts, query);

        builder.Query = String.Join("&", queryParts);
        return builder.Uri;
    }

    private static void AddExistingQuery(List<String> queryParts, String query)
    {
        var value = query.StartsWith("?", StringComparison.Ordinal) ? query[1..] : query;

        if (!String.IsNullOrWhiteSpace(value))
        {
            queryParts.Add(value);
        }
    }

    private static void AddConfiguredQuery(List<String> queryParts, Dictionary<String, String?> query)
    {
        foreach (var (name, value) in query)
        {
            AddQueryValue(queryParts, name, value);
        }
    }

    private static void AddRuntimeQuery(List<String> queryParts, Dictionary<String, JsonElement>? query)
    {
        if (query is null)
        {
            return;
        }

        foreach (var (name, value) in query)
        {
            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    AddQueryValue(queryParts, name, ConvertQueryValue(item));
                }
            }
            else
            {
                AddQueryValue(queryParts, name, ConvertQueryValue(value));
            }
        }
    }

    private static void AddQueryValue(List<String> queryParts, String name, String? value)
    {
        if (String.IsNullOrWhiteSpace(name) || value is null)
        {
            return;
        }

        queryParts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
    }

    private static String? ConvertQueryValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.GetRawText(),
            _ => value.GetRawText()
        };
    }

    private static HttpContent? CreateContent(JsonElement? body, String? contentType)
    {
        if (body is null || body.Value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var mediaType = String.IsNullOrWhiteSpace(contentType) ? "application/json" : contentType.Trim();
        var text = body.Value.ValueKind == JsonValueKind.String && !IsJsonContentType(mediaType)
            ? body.Value.GetString() ?? ""
            : body.Value.GetRawText();

        return new StringContent(text, Encoding.UTF8, mediaType);
    }

    private static void AddAuthorization(HttpRequestMessage request, HttpApiEndpointOptions endpoint)
    {
        if (!String.IsNullOrWhiteSpace(endpoint.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.BearerToken.Trim());
            return;
        }

        if (!String.IsNullOrWhiteSpace(endpoint.BasicToken))
        {
            var parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes(endpoint.BasicToken.Trim()));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", parameter);
            return;
        }

        if (!String.IsNullOrWhiteSpace(endpoint.Authorization))
        {
            request.Headers.TryAddWithoutValidation("Authorization", endpoint.Authorization.Trim());
        }
    }

    private static void AddHeaders(HttpRequestMessage request, Dictionary<String, String?>? headers)
    {
        if (headers is null)
        {
            return;
        }

        foreach (var (name, value) in headers)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("HTTP API header names cannot be empty.");
            }

            if (value is null)
            {
                throw new InvalidOperationException($"HTTP API header {name} cannot have a null value.");
            }

            if (name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                && request.Headers.Contains("Authorization"))
            {
                throw new InvalidOperationException("Authorization can be configured once per HTTP API request.");
            }

            if (request.Headers.TryAddWithoutValidation(name, value))
            {
                continue;
            }

            if (request.Content is not null && request.Content.Headers.TryAddWithoutValidation(name, value))
            {
                continue;
            }

            throw new InvalidOperationException($"HTTP API header {name} is not valid for this request.");
        }
    }

    private static Dictionary<String, String[]> GetResponseHeaders(HttpResponseMessage response)
    {
        return response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(
                header => header.Key,
                header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<(Object Body, Boolean Truncated)> ReadBodyAsync(
        HttpResponseMessage response,
        Int32 maxResponseBytes,
        CancellationToken cancellationToken)
    {
        var byteLimit = Math.Clamp(maxResponseBytes, 1, AbsoluteMaxResponseBytes);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var (bytes, truncated) = await ReadBytesAsync(stream, byteLimit, cancellationToken);

        if (bytes.Length == 0)
        {
            return (new { kind = "empty" }, truncated);
        }

        var contentType = response.Content.Headers.ContentType;
        var mediaType = contentType?.MediaType ?? "";

        if (IsJsonContentType(mediaType))
        {
            var text = DecodeText(bytes, contentType?.CharSet);

            try
            {
                using var document = JsonDocument.Parse(text);
                return (new { kind = "json", json = document.RootElement.Clone() }, truncated);
            }
            catch (JsonException)
            {
                return (new { kind = "text", text }, truncated);
            }
        }

        if (IsTextContentType(mediaType))
        {
            return (new { kind = "text", text = DecodeText(bytes, contentType?.CharSet) }, truncated);
        }

        return (new { kind = "base64", base64 = Convert.ToBase64String(bytes) }, truncated);
    }

    private static async Task<(Byte[] Bytes, Boolean Truncated)> ReadBytesAsync(
        Stream stream,
        Int32 byteLimit,
        CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new Byte[81920];
        var truncated = false;

        while (memory.Length < byteLimit)
        {
            var remaining = byteLimit - (Int32)memory.Length;
            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), cancellationToken);

            if (read == 0)
            {
                break;
            }

            memory.Write(buffer, 0, read);
        }

        if (memory.Length == byteLimit)
        {
            truncated = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken) > 0;
        }

        return (memory.ToArray(), truncated);
    }

    private static String DecodeText(Byte[] bytes, String? charset)
    {
        if (!String.IsNullOrWhiteSpace(charset))
        {
            try
            {
                return Encoding.GetEncoding(charset).GetString(bytes);
            }
            catch (ArgumentException)
            {
            }
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static Boolean IsJsonContentType(String? mediaType)
    {
        if (String.IsNullOrWhiteSpace(mediaType))
        {
            return false;
        }

        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static Boolean IsTextContentType(String? mediaType)
    {
        if (String.IsNullOrWhiteSpace(mediaType))
        {
            return false;
        }

        return mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
    }

    private static String GetRequiredValue(String value, String message)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
    }

    private static String ToJson(Object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}

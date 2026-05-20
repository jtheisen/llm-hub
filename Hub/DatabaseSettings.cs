namespace Hub;

internal sealed class DatabaseSettings
{
    public List<DatabaseConnectionOptions> Connections { get; init; } = [];
}

internal sealed class DatabaseConnectionOptions
{
    public String Name { get; init; } = "";

    public String Provider { get; init; } = "postgres";

    public String? Host { get; init; }

    public Int32? Port { get; init; }

    public String? Database { get; init; }

    public String? Username { get; init; }

    public String? Password { get; init; }

    public String? SslMode { get; init; }

    public Boolean? TrustServerCertificate { get; init; }

    public Dictionary<String, String?> Options { get; init; } = [];
}

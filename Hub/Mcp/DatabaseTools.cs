using System.ComponentModel;
using System.Data.Common;
using System.Text.Json;
using Npgsql;

namespace Hub.Mcp;

[ModelContextProtocol.Server.McpServerToolType]
internal sealed class DatabaseTools(DatabaseSettings settings)
{
    private const String PostgresProviderName = "postgres";
    private const Int32 DefaultMaxRows = 200;
    private const Int32 AbsoluteMaxRows = 5000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Lists configured database connections without exposing passwords or secrets.")]
    public String ListConfiguredDatabaseConnections()
    {
        var connections = settings.Connections
            .Select(connection => new
            {
                connection.Name,
                Provider = GetProvider(connection),
                connection.Host,
                Port = connection.Port ?? 5432,
                connection.Database,
                connection.Username,
                PasswordConfigured = !String.IsNullOrWhiteSpace(connection.Password),
                Options = connection.Options.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray()
            })
            .ToArray();

        return ToJson(new
        {
            connections
        });
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Runs a SQL query against a configured database connection and returns JSON with columns, rows, row count, and truncation status. Use @parameterName placeholders and pass matching parameters as a JSON object.")]
    public async Task<String> QueryDatabase(
        [Description("The configured connection name.")]
        String connectionName,
        [Description("The SQL query to run.")]
        String sql,
        [Description("Optional named SQL parameters as a JSON object. Parameter names may include or omit the @ prefix.")]
        Dictionary<String, JsonElement>? parameters = null,
        [Description("Maximum rows to return. Defaults to 200 and is capped at 5000.")]
        Int32 maxRows = DefaultMaxRows,
        [Description("Command timeout in seconds. Defaults to 30.")]
        Int32 commandTimeoutSeconds = 30)
    {
        var connectionOptions = GetConnection(connectionName);
        var rowLimit = Math.Clamp(maxRows, 1, AbsoluteMaxRows);

        await using var connection = CreateConnection(connectionOptions);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = Math.Max(commandTimeoutSeconds, 1);
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        var columns = Enumerable
            .Range(0, reader.FieldCount)
            .Select(index => new
            {
                Name = reader.GetName(index),
                Type = reader.GetDataTypeName(index)
            })
            .ToArray();
        var rows = new List<Dictionary<String, Object?>>();

        while (rows.Count < rowLimit && await reader.ReadAsync())
        {
            var row = new Dictionary<String, Object?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < reader.FieldCount; index++)
            {
                var value = await reader.IsDBNullAsync(index) ? null : reader.GetValue(index);
                row[reader.GetName(index)] = NormalizeValue(value);
            }

            rows.Add(row);
        }

        var truncated = await reader.ReadAsync();

        return ToJson(new
        {
            connection = connectionOptions.Name,
            provider = GetProvider(connectionOptions),
            columns,
            rows,
            rowCount = rows.Count,
            truncated
        });
    }

    [ModelContextProtocol.Server.McpServerTool]
    [Description("Executes a SQL command against a configured database connection and returns JSON with the affected row count. Use @parameterName placeholders and pass matching parameters as a JSON object.")]
    public async Task<String> ExecuteDatabase(
        [Description("The configured connection name.")]
        String connectionName,
        [Description("The SQL command to execute.")]
        String sql,
        [Description("Optional named SQL parameters as a JSON object. Parameter names may include or omit the @ prefix.")]
        Dictionary<String, JsonElement>? parameters = null,
        [Description("Command timeout in seconds. Defaults to 30.")]
        Int32 commandTimeoutSeconds = 30)
    {
        var connectionOptions = GetConnection(connectionName);

        await using var connection = CreateConnection(connectionOptions);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = Math.Max(commandTimeoutSeconds, 1);
        AddParameters(command, parameters);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return ToJson(new
        {
            connection = connectionOptions.Name,
            provider = GetProvider(connectionOptions),
            rowsAffected
        });
    }

    private DatabaseConnectionOptions GetConnection(String connectionName)
    {
        var matches = settings.Connections
            .Where(connection => connection.Name.Equals(connectionName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            throw new InvalidOperationException($"No database connection named '{connectionName}' is configured.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Multiple database connections named '{connectionName}' are configured.");
        }

        var connection = matches[0];
        var provider = GetProvider(connection);

        if (!provider.Equals(PostgresProviderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Database provider '{provider}' is not supported yet.");
        }

        return connection;
    }

    private static String GetProvider(DatabaseConnectionOptions connection)
    {
        return String.IsNullOrWhiteSpace(connection.Provider)
            ? PostgresProviderName
            : connection.Provider.Trim();
    }

    private static NpgsqlConnection CreateConnection(DatabaseConnectionOptions connection)
    {
        var builder = new NpgsqlConnectionStringBuilder();

        SetIfConfigured(builder, "Host", connection.Host);
        SetIfConfigured(builder, "Port", connection.Port);
        SetIfConfigured(builder, "Database", connection.Database);
        SetIfConfigured(builder, "Username", connection.Username);
        SetIfConfigured(builder, "Password", connection.Password);
        SetIfConfigured(builder, "SSL Mode", connection.SslMode);
        SetIfConfigured(builder, "Trust Server Certificate", connection.TrustServerCertificate);

        foreach (var (key, value) in connection.Options)
        {
            SetIfConfigured(builder, key, value);
        }

        return new NpgsqlConnection(builder.ConnectionString);
    }

    private static void SetIfConfigured(DbConnectionStringBuilder builder, String key, Object? value)
    {
        if (value is null)
        {
            return;
        }

        if (value is String text && String.IsNullOrWhiteSpace(text))
        {
            return;
        }

        builder[key] = value;
    }

    private static void AddParameters(NpgsqlCommand command, Dictionary<String, JsonElement>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var (name, value) in parameters)
        {
            var parameterName = name.StartsWith("@", StringComparison.Ordinal) ? name : $"@{name}";
            command.Parameters.AddWithValue(parameterName, ConvertParameterValue(value));
        }
    }

    private static Object ConvertParameterValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => DBNull.Value,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String => (Object?)value.GetString() ?? DBNull.Value,
            JsonValueKind.Array or JsonValueKind.Object => value.GetRawText(),
            _ => DBNull.Value
        };
    }

    private static Object? NormalizeValue(Object? value)
    {
        return value switch
        {
            null => null,
            DBNull => null,
            DateOnly date => date.ToString("O"),
            TimeOnly time => time.ToString("O"),
            TimeSpan timeSpan => timeSpan.ToString(),
            Byte[] bytes => Convert.ToBase64String(bytes),
            String or Boolean or Char or SByte or Byte or Int16 or Int32 or Int64 or UInt16 or UInt32 or UInt64 or Single or Double or Decimal or DateTime or DateTimeOffset or Guid => value,
            _ => value.ToString()
        };
    }

    private static String ToJson(Object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}

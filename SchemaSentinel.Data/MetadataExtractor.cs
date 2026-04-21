using Microsoft.Data.SqlClient;
using SchemaSentinel.Core.Models;
using SchemaSentinel.Data.Queries;

namespace SchemaSentinel.Data;

public class MetadataExtractor
{
    private readonly SqlConnectionService _connectionService;

    public MetadataExtractor(SqlConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IList<TableModel>> ExtractTablesAsync(
        ConnectionProfile profile,
        ComparisonOptions options,
        CancellationToken cancellationToken = default)
    {
        var tables = new List<TableModel>();
        await using var connection = await _connectionService.CreateAndOpenConnectionAsync(profile, cancellationToken);

        var tableRows = await ExecuteQueryAsync(connection, TableQueries.GetTables(options), cancellationToken);

        foreach (var row in tableRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var table = new TableModel
            {
                SchemaName = row["SchemaName"].ToString()!,
                TableName = row["TableName"].ToString()!,
                ModifyDate = row["ModifyDate"] is DBNull ? null : (DateTime?)row["ModifyDate"]
            };

            table.Columns = await ExtractColumnsAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            table.PrimaryKey = await ExtractPrimaryKeyAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            tables.Add(table);
        }

        return tables;
    }

    public async Task<IList<SqlModuleModel>> ExtractModulesAsync(
        ConnectionProfile profile,
        ComparisonOptions options,
        CancellationToken cancellationToken = default)
    {
        var modules = new List<SqlModuleModel>();
        await using var connection = await _connectionService.CreateAndOpenConnectionAsync(profile, cancellationToken);

        var rows = await ExecuteQueryAsync(connection, ModuleQueries.GetModules(options), cancellationToken);

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            modules.Add(new SqlModuleModel
            {
                SchemaName = row["SchemaName"].ToString()!,
                ObjectName = row["ObjectName"].ToString()!,
                ModuleType = ParseModuleType(row["ObjectType"].ToString()!),
                Definition = row["Definition"]?.ToString() ?? string.Empty,
                ModifyDate = row["ModifyDate"] is DBNull ? null : (DateTime?)row["ModifyDate"]
            });
        }

        return modules;
    }

    private async Task<List<ColumnModel>> ExtractColumnsAsync(
        SqlConnection connection, string schema, string table, CancellationToken ct)
    {
        var rows = await ExecuteQueryAsync(connection, TableQueries.GetColumns(schema, table), ct);
        return rows.Select(row => new ColumnModel
        {
            Name = row["ColumnName"].ToString()!,
            DataType = row["DataType"].ToString()!,
            MaxLength = row["MaxLength"] is DBNull ? null : Convert.ToInt32(row["MaxLength"]),
            Precision = row["Precision"] is DBNull ? null : Convert.ToInt32(row["Precision"]),
            Scale = row["Scale"] is DBNull ? null : Convert.ToInt32(row["Scale"]),
            IsNullable = Convert.ToBoolean(row["IsNullable"]),
            IsIdentity = Convert.ToBoolean(row["IsIdentity"]),
            IdentitySeed = row["IdentitySeed"] is DBNull ? null : Convert.ToInt32(row["IdentitySeed"]),
            IdentityIncrement = row["IdentityIncrement"] is DBNull ? null : Convert.ToInt32(row["IdentityIncrement"]),
            IsComputed = Convert.ToBoolean(row["IsComputed"]),
            ComputedDefinition = row["ComputedDefinition"] is DBNull ? null : row["ComputedDefinition"].ToString(),
            DefaultDefinition = row["DefaultDefinition"] is DBNull ? null : row["DefaultDefinition"].ToString(),
            OrdinalPosition = Convert.ToInt32(row["OrdinalPosition"])
        }).ToList();
    }

    private async Task<PrimaryKeyModel?> ExtractPrimaryKeyAsync(
        SqlConnection connection, string schema, string table, CancellationToken ct)
    {
        var rows = await ExecuteQueryAsync(connection, TableQueries.GetPrimaryKey(schema, table), ct);
        if (!rows.Any()) return null;

        return new PrimaryKeyModel
        {
            ConstraintName = rows[0]["ConstraintName"].ToString()!,
            IsClustered = rows[0]["IsClustered"].ToString()?.Equals("CLUSTERED", StringComparison.OrdinalIgnoreCase) == true,
            Columns = rows.Select(r => r["ColumnName"].ToString()!).ToList()
        };
    }

    private static ModuleType ParseModuleType(string objectType) =>
        objectType.Trim().ToUpperInvariant() switch
        {
            "V"              => ModuleType.View,
            "P"              => ModuleType.Procedure,
            "FN" or "IF" or "TF" => ModuleType.Function,
            _                => ModuleType.Procedure
        };

    private static async Task<List<Dictionary<string, object>>> ExecuteQueryAsync(
        SqlConnection connection, string sql, CancellationToken ct)
    {
        var results = new List<Dictionary<string, object>>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.GetValue(i);
            results.Add(row);
        }

        return results;
    }
}

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace vector_app_local.Data;

public static class SchemaBaselineInitializer
{
    public static async Task CreateCurrentSchemaBaselineAsync(VectorDbContext db, IMigrationsAssembly migrationsAssembly)
    {
        if (!db.Database.IsSqlServer())
        {
            throw new InvalidOperationException("Schema baseline initialization is only supported for Azure SQL / SQL Server.");
        }

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        var existingTables = await GetExistingTablesAsync(connection);
        var productTables = existingTables
            .Where(table => !string.Equals(table, "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (productTables.Count > 0)
        {
            throw new InvalidOperationException(
                "Schema baseline initialization refused to run because product tables already exist.");
        }

        await ExecuteNonQueryAsync(connection, """
            IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (SELECT 1 FROM [__EFMigrationsHistory])
                    THROW 51000, 'Schema baseline initialization refused to run because migration history already exists.', 1;

                DROP TABLE [__EFMigrationsHistory];
            END
            """);

        var createScript = db.Database.GenerateCreateScript();
        await ExecuteSqlScriptAsync(connection, createScript);

        await ExecuteNonQueryAsync(connection, """
            IF OBJECT_ID(N'[__EFMigrationsHistory]', N'U') IS NULL
            BEGIN
                CREATE TABLE [__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END
            """);

        var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "8.0.0";
        foreach (var migrationId in migrationsAssembly.Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            await ExecuteNonQueryAsync(
                connection,
                """
                IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = @migrationId)
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES (@migrationId, @productVersion);
                END
                """,
                CreateParameter(connection, "@migrationId", migrationId),
                CreateParameter(connection, "@productVersion", productVersion));
        }
    }

    private static async Task<List<string>> GetExistingTablesAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME
            """;

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task ExecuteSqlScriptAsync(DbConnection connection, string script)
    {
        foreach (var batch in SplitSqlBatches(script))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await ExecuteNonQueryAsync(connection, batch);
        }
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        var lines = script.Replace("\r\n", "\n").Split('\n');
        var current = new List<string>();
        foreach (var line in lines)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return string.Join(Environment.NewLine, current);
                current.Clear();
                continue;
            }

            current.Add(line);
        }

        if (current.Count > 0)
        {
            yield return string.Join(Environment.NewLine, current);
        }
    }

    private static async Task ExecuteNonQueryAsync(DbConnection connection, string commandText, params DbParameter[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static DbParameter CreateParameter(DbConnection connection, string name, object value)
    {
        var parameter = connection.CreateCommand().CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        return parameter;
    }
}

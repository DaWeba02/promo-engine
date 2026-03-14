using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace PromoEngine.IntegrationTests.Infrastructure;

public sealed class PromoEngineApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string SqlConnectionEnvVar = "ConnectionStrings__SqlServer";
    private MsSqlContainer? _sqlContainer;

    public bool DockerUnavailable { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        try
        {
            _sqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

            await _sqlContainer.StartAsync();
            var connectionString = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString())
            {
                TrustServerCertificate = true,
                Encrypt = false,
                ConnectTimeout = 30
            }.ConnectionString;

            await WaitForSqlServerAsync(connectionString);
            Environment.SetEnvironmentVariable(SqlConnectionEnvVar, connectionString);
        }
        catch
        {
            DockerUnavailable = true;
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable(SqlConnectionEnvVar, null);

        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }
    }

    private static async Task WaitForSqlServerAsync(string connectionString)
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < TimeSpan.FromMinutes(2))
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();
                return;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new TimeoutException("SQL Server container did not become reachable in time.");
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<PromoEngineApiFactory>;

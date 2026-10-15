using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Splitly.Api.Data;
using Testcontainers.PostgreSql;

namespace Splitly.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory against a throwaway PostgreSQL container that is shared by all tests.
/// Tests isolate themselves by registering fresh users, so the database never needs resetting.
/// </summary>
public sealed class SplitlyApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _database;

    /// <summary>Set when Docker can't be reached; the tests are then skipped instead of failing.</summary>
    public string? DockerUnavailableReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _database = new PostgreSqlBuilder("postgres:18-alpine").Build();
        }
        catch (DockerUnavailableException ex)
        {
            DockerUnavailableReason = ex.Message;
            return;
        }

        await _database.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Splitly", _database?.GetConnectionString());
    }

    public override async ValueTask DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}

[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<SplitlyApiFactory>;

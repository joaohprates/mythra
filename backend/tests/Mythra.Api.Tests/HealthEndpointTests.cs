using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mythra.Infrastructure.Persistence;

namespace Mythra.Api.Tests;

public class HealthEndpointTests : IClassFixture<MythraTestFactory>
{
    private readonly MythraTestFactory _factory;

    public HealthEndpointTests(MythraTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200_and_payload()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"status\":\"ok\"");
        body.Should().Contain("\"service\":\"mythra\"");
    }

    [Fact]
    public async Task Ping_returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/ping");
        response.EnsureSuccessStatusCode();
    }
}

public sealed class MythraTestFactory : WebApplicationFactory<Program>
{
    // Open the connection at construction time so it persists for the lifetime of the factory
    private readonly SqliteConnection _conn;

    public MythraTestFactory()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Swap the registered SQLite connection string with our in-memory one
            var dbContextOptions = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<MythraDbContext>));
            if (dbContextOptions is not null) services.Remove(dbContextOptions);

            // EnsureDatabaseAsync in Program.cs runs Migrate() automatically on startup
            services.AddDbContext<MythraDbContext>(opt => opt.UseSqlite(_conn));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conn.Dispose();
    }
}

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Gametopia.Domain.Infrastructure.Persistence;

namespace Gametopia.Domain.Tests.Configuration;

public class SwaggerConfigTests
{
    [Fact]
    public async Task Swagger_should_be_enabled_when_local_settings_enable_it()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Local");
                builder.UseContentRoot(GetApiProjectPath());
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDatabaseInitializer>();
                    services.AddSingleton<IDatabaseInitializer>(new NoOpDatabaseInitializer());
                    services.RemoveAll<GametopiaDomainDbContext>();
                    services.RemoveAll<DbContextOptions<GametopiaDomainDbContext>>();
                    services.RemoveAll<IDatabaseProvider>();
                    services.RemoveAll<IDbContextOptionsConfiguration<GametopiaDomainDbContext>>();
                    services.AddDbContext<GametopiaDomainDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                });
            });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_should_be_disabled_when_setting_is_false()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Local");
                builder.UseContentRoot(GetApiProjectPath());
                builder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Swagger:Enabled"] = "false"
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDatabaseInitializer>();
                    services.AddSingleton<IDatabaseInitializer>(new NoOpDatabaseInitializer());
                    services.RemoveAll<GametopiaDomainDbContext>();
                    services.RemoveAll<DbContextOptions<GametopiaDomainDbContext>>();
                    services.RemoveAll<IDatabaseProvider>();
                    services.RemoveAll<IDbContextOptionsConfiguration<GametopiaDomainDbContext>>();
                    services.AddDbContext<GametopiaDomainDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                });
            });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string GetApiProjectPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "Gametopia.Domain.Api"));
    }
}

internal sealed class NoOpDatabaseInitializer : IDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

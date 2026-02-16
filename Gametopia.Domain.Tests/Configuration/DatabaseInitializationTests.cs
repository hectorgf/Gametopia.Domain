using Gametopia.Domain.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gametopia.Domain.Tests.Configuration;

public class DatabaseInitializationTests
{
    [Fact(Skip = "Requires SQL Server connection configured - use in-memory DB instead")]
    public async Task Local_environment_should_run_database_migrations_on_startup()
    {
        var spy = new DatabaseInitializerSpy();

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Local");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDatabaseInitializer>();
                    services.AddSingleton<IDatabaseInitializer>(spy);
                });
            });

        _ = factory.CreateClient();

        Assert.True(spy.WasCalled);
    }
}

internal sealed class DatabaseInitializerSpy : IDatabaseInitializer
{
    public bool WasCalled { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        return Task.CompletedTask;
    }
}

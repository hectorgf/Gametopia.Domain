using Gametopia.Domain.Domain.Users;
using Gametopia.Domain.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Gametopia.Domain.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Gametopia.Domain.Tests.Users;

public class RoleSeedingTests
{
    [Fact]
    public async Task Local_should_seed_roles_admin_verificated_user()
    {
        await using var factory = BuildFactory();
        _ = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IRoleAndUserSeeder>();
        await seeder.SeedAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        Assert.True(await roleManager.RoleExistsAsync("admin"));
        Assert.True(await roleManager.RoleExistsAsync("verificated"));
        Assert.True(await roleManager.RoleExistsAsync("user"));
    }

    [Fact]
    public async Task Local_should_seed_default_users_with_expected_roles()
    {
        await using var factory = BuildFactory();
        _ = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IRoleAndUserSeeder>();
        await seeder.SeedAsync();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await userManager.FindByEmailAsync("admin@gametopia.com");
        var verified = await userManager.FindByEmailAsync("verificated@gametopia.com");
        var basic = await userManager.FindByEmailAsync("user@gametopia.com");

        Assert.NotNull(admin);
        Assert.NotNull(verified);
        Assert.NotNull(basic);

        Assert.True(await userManager.IsInRoleAsync(admin!, "admin"));
        Assert.True(await userManager.IsInRoleAsync(verified!, "verificated"));
        Assert.True(await userManager.IsInRoleAsync(basic!, "user"));
    }

    private static WebApplicationFactory<Program> BuildFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Local");
                builder.UseContentRoot(Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "Gametopia.Domain.Api")));
                builder.UseSetting("environment", "Local");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<GametopiaDomainDbContext>();
                    services.RemoveAll<DbContextOptions<GametopiaDomainDbContext>>();
                    services.RemoveAll<IDatabaseInitializer>();
                    services.RemoveAll<IDatabaseProvider>();
                    services.RemoveAll<IDbContextOptionsConfiguration<GametopiaDomainDbContext>>();

                    services.AddDbContext<GametopiaDomainDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

                    services.AddIdentityCore<ApplicationUser>()
                        .AddRoles<IdentityRole<Guid>>()
                        .AddEntityFrameworkStores<GametopiaDomainDbContext>();

                    services.AddSingleton<IDatabaseInitializer>(new NoOpDatabaseInitializer());
                    services.AddScoped<IRoleAndUserSeeder, RoleAndUserSeeder>();
                });
            });
    }
}

internal sealed class NoOpDatabaseInitializer : IDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

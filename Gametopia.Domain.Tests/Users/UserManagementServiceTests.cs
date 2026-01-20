using Gametopia.Domain.Application.Users;
using Gametopia.Domain.Domain.Users;
using Gametopia.Domain.Infrastructure.Persistence;
using Gametopia.Domain.Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gametopia.Domain.Tests.Users;

public class UserManagementServiceTests
{
    [Fact]
    public async Task Create_user_should_create_profile_when_request_is_valid()
    {
        var services = new ServiceCollection();

        services.AddDbContext<GametopiaDomainDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<GametopiaDomainDbContext>();

        services.AddScoped<IUserManagementService, UserManagementService>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<GametopiaDomainDbContext>();

        var result = await service.CreateUserAsync(new CreateUserRequest
        {
            Email = "test@gametopia.local",
            UserName = "testuser",
            Password = "P@ssw0rd!",
            DisplayName = "Test User"
        });

        var profile = await dbContext.UserProfiles.SingleOrDefaultAsync(p => p.UserId == Guid.Parse(result.UserId!));

        Assert.NotNull(profile);
        Assert.Equal("Test User", profile!.DisplayName);
    }

    [Fact]
    public async Task Create_user_should_return_failure_when_password_is_invalid()
    {
        var services = new ServiceCollection();

        services.AddDbContext<GametopiaDomainDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<GametopiaDomainDbContext>();

        services.AddScoped<IUserManagementService, UserManagementService>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        var result = await service.CreateUserAsync(new CreateUserRequest
        {
            Email = "test2@gametopia.local",
            UserName = "testuser2",
            Password = "weak"
        });

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }
}

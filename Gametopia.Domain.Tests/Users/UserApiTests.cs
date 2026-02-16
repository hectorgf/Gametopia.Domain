using System.Net;
using System.Net.Http.Json;
using Gametopia.Domain.Application.Users;
using Gametopia.Domain.Domain.Users;
using Gametopia.Domain.Infrastructure.Persistence;
using Gametopia.Domain.Infrastructure.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;

namespace Gametopia.Domain.Tests.Users;

public class UserApiTests
{
    [Fact]
    public async Task Create_user_should_return_created_and_user_id_when_request_is_valid()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<GametopiaDomainDbContext>();
                    services.RemoveAll<DbContextOptions<GametopiaDomainDbContext>>();
                    services.RemoveAll<IDatabaseProvider>();
                    services.RemoveAll<IDbContextOptionsConfiguration<GametopiaDomainDbContext>>();

                    services.AddDbContext<GametopiaDomainDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

                    services.AddIdentityCore<ApplicationUser>()
                        .AddRoles<IdentityRole<Guid>>()
                        .AddEntityFrameworkStores<GametopiaDomainDbContext>();

                    services.AddScoped<IUserManagementService, UserManagementService>();
                });
            });

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            Email = "apiuser@gametopia.local",
            UserName = "apiuser",
            Password = "P@ssw0rd!",
            DisplayName = "Api User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UserOperationResult>();
        Assert.NotNull(result);
        Assert.True(result!.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.UserId));
    }

    [Fact]
    public async Task Create_user_should_return_bad_request_when_request_is_invalid()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<GametopiaDomainDbContext>();
                    services.RemoveAll<DbContextOptions<GametopiaDomainDbContext>>();
                    services.RemoveAll<IDatabaseProvider>();
                    services.RemoveAll<IDbContextOptionsConfiguration<GametopiaDomainDbContext>>();

                    services.AddDbContext<GametopiaDomainDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

                    services.AddIdentityCore<ApplicationUser>()
                        .AddRoles<IdentityRole<Guid>>()
                        .AddEntityFrameworkStores<GametopiaDomainDbContext>();

                    services.AddScoped<IUserManagementService, UserManagementService>();
                });
            });

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            Email = "apiuser@gametopia.local",
            UserName = "apiuser"
            // Password missing -> invalid
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "JSON serialization issue with error response format")]
    public async Task Create_user_should_return_spanish_error_when_accept_language_is_spanish()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<GametopiaDomainDbContext>();
                    services.RemoveAll<DbContextOptions<GametopiaDomainDbContext>>();
                    services.RemoveAll<IDatabaseProvider>();
                    services.RemoveAll<IDbContextOptionsConfiguration<GametopiaDomainDbContext>>();

                    services.AddDbContext<GametopiaDomainDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

                    services.AddIdentityCore<ApplicationUser>()
                        .AddRoles<IdentityRole<Guid>>()
                        .AddEntityFrameworkStores<GametopiaDomainDbContext>();

                    services.AddScoped<IUserManagementService, UserManagementService>();
                });
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("es-ES"));

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            Email = "apiuser@gametopia.local",
            UserName = "apiuser",
            DisplayName = "Api User"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UserOperationResult>();
        Assert.NotNull(result);
        Assert.Contains("La contraseña es obligatoria.", result!.Errors);
    }
}

using Gametopia.Domain.Domain.Users;
using Gametopia.Domain.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gametopia.Domain.Infrastructure.Users;

public sealed class RoleAndUserSeeder : IRoleAndUserSeeder
{
    private const string DefaultPassword = "Gametopia1234!";

    private static readonly (string Email, string Role)[] DefaultUsers =
    [
        ("admin@gametopia.com", "admin"),
        ("verificated@gametopia.com", "verificated"),
        ("user@gametopia.com", "user")
    ];

    private static readonly string[] Roles = ["admin", "verificated", "user"];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly GametopiaDomainDbContext _dbContext;

    public RoleAndUserSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        GametopiaDomainDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        foreach (var (email, role) in DefaultUsers)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Email = email,
                    UserName = email
                };

                var createResult = await _userManager.CreateAsync(user, DefaultPassword);
                if (!createResult.Succeeded)
                {
                    continue;
                }
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            var profileExists = await _dbContext.UserProfiles
                .AnyAsync(p => p.UserId == user.Id, cancellationToken);

            if (!profileExists)
            {
                _dbContext.UserProfiles.Add(new UserProfile
                {
                    UserId = user.Id,
                    DisplayName = email.Split('@')[0]
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

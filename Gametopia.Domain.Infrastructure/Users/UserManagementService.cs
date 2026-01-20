using Gametopia.Domain.Application.Users;
using Gametopia.Domain.Domain.Users;
using Gametopia.Domain.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gametopia.Domain.Infrastructure.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GametopiaDomainDbContext _dbContext;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        GametopiaDomainDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<UserOperationResult> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.UserName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return UserOperationResult.Failed(result.Errors.Select(e => e.Description).ToArray());
        }

        var profileExists = await _dbContext.UserProfiles
            .AnyAsync(p => p.UserId == user.Id);

        if (!profileExists)
        {
            var profile = new UserProfile
            {
                UserId = user.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                Bio = request.Bio
            };

            _dbContext.UserProfiles.Add(profile);
            await _dbContext.SaveChangesAsync();
        }

        return UserOperationResult.Success(user.Id);
    }

    public async Task<UserOperationResult> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _userManager.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return UserOperationResult.Failed("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            user.UserName = request.UserName;
        }

        if (user.Profile is null)
        {
            user.Profile = new UserProfile { UserId = user.Id };
            _dbContext.UserProfiles.Add(user.Profile);
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            user.Profile.FirstName = request.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            user.Profile.LastName = request.LastName;
        }

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.Profile.DisplayName = request.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(request.Bio))
        {
            user.Profile.Bio = request.Bio;
        }
        if (!string.IsNullOrWhiteSpace(request.SteamProfile))
        {
            user.Profile.SteamProfile = request.SteamProfile;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return UserOperationResult.Failed(result.Errors.Select(e => e.Description).ToArray());
        }

        await _dbContext.SaveChangesAsync();

        return UserOperationResult.Success(user.Id);
    }

    public async Task<UserOperationResult> SoftDeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserOperationResult.Failed("User not found.");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return UserOperationResult.Failed(result.Errors.Select(e => e.Description).ToArray());
        }

        return UserOperationResult.Success(user.Id);
    }

    public async Task<UserOperationResult> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return UserOperationResult.Failed("User not found.");
        }

        if (user.Profile is not null)
        {
            _dbContext.UserProfiles.Remove(user.Profile);
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return UserOperationResult.Failed(result.Errors.Select(e => e.Description).ToArray());
        }

        await _dbContext.SaveChangesAsync();

        return UserOperationResult.Success(user.Id);
    }
}

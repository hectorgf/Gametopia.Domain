using Gametopia.Domain.Application.Users;
using Gametopia.Domain.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gametopia.Domain.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(IUserManagementService userManagementService, UserManager<ApplicationUser> userManager)
    {
        _userManagementService = userManagementService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<ActionResult<UserOperationResult>> Create([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userManagementService.CreateUserAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<UserOperationResult>> Update(Guid userId, [FromBody] UpdateUserRequest request)
    {
        var result = await _userManagementService.UpdateUserAsync(userId, request);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{userId:guid}/soft-delete")]
    public async Task<ActionResult<UserOperationResult>> SoftDelete(Guid userId)
    {
        var result = await _userManagementService.SoftDeleteUserAsync(userId);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<ActionResult<UserOperationResult>> Delete(Guid userId)
    {
        var result = await _userManagementService.DeleteUserAsync(userId);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email)
                   ?? await _userManager.FindByNameAsync(request.Email);

        if (user is null)
        {
            return BadRequest(UserOperationResult.Failed("Invalid credentials."));
        }

        var ok = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!ok)
        {
            return BadRequest(UserOperationResult.Failed("Invalid credentials."));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true });

        return Ok(UserOperationResult.Success(user.Id));
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetCurrent()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(idClaim)) return Unauthorized();

        if (!Guid.TryParse(idClaim, out var guid)) return Unauthorized();

        var user = await _userManager.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == guid);

        if (user is null) return Unauthorized();

        var steamProfile = user.Profile?.SteamProfile;
        var steamVerified = false;
        if (!string.IsNullOrWhiteSpace(steamProfile))
        {
            try
            {
                // simple pattern: accept steamcommunity urls (/id/ or /profiles/) or numeric id
                var regex = new System.Text.RegularExpressions.Regex(@"(steamcommunity\.com\/(id|profiles)\/[^\s\/]+|^[0-9]{17}$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                steamVerified = regex.IsMatch(steamProfile);
            }
            catch
            {
                steamVerified = false;
            }
        }

        return Ok(new
        {
            userId = user.Id,
            userName = user.UserName,
            email = user.Email,
            displayName = user.Profile?.DisplayName,
            firstName = user.Profile?.FirstName,
            lastName = user.Profile?.LastName,
            bio = user.Profile?.Bio,
            steamProfile = steamProfile,
            steamVerified = steamVerified,
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}

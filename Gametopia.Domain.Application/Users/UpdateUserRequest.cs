namespace Gametopia.Domain.Application.Users;

public sealed class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? UserName { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? SteamProfile { get; set; }
}

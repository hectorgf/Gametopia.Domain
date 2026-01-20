namespace Gametopia.Domain.Domain.Users;

public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? SteamProfile { get; set; }

    public ApplicationUser? User { get; set; }
}

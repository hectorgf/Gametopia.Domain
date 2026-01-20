namespace Gametopia.Domain.Application.Users;

public sealed class LoginRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Email { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string Password { get; set; } = string.Empty;
}

namespace Gametopia.Domain.Application.Users;

public sealed class CreateUserRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Email { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string UserName { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
}

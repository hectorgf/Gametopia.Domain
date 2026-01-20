using Microsoft.AspNetCore.Identity;

namespace Gametopia.Domain.Domain.Users;

public class ApplicationUser : IdentityUser<Guid>
{
    public UserProfile? Profile { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

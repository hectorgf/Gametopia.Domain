namespace Gametopia.Domain.Infrastructure.Users;

public interface IRoleAndUserSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

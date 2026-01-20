using Microsoft.EntityFrameworkCore;

namespace Gametopia.Domain.Infrastructure.Persistence;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly GametopiaDomainDbContext _dbContext;

    public DatabaseInitializer(GametopiaDomainDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
    }
}

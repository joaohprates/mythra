using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mythra.Infrastructure.Persistence;

public sealed class MythraDbContextFactory : IDesignTimeDbContextFactory<MythraDbContext>
{
    public MythraDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MythraDbContext>()
            .UseSqlite("Data Source=mythra.design.db")
            .Options;
        return new MythraDbContext(options);
    }
}

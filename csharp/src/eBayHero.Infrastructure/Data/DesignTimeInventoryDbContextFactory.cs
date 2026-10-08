using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace eBayHero.Infrastructure.Data;

/// <summary>
/// Used only by the EF Core tooling (<c>dotnet ef migrations add</c>). At runtime the
/// context is created through dependency injection in <c>AddInventoryInfrastructure</c>.
/// The connection string here is never used for real data; it only gives the tooling a
/// provider so migrations can be scaffolded.
/// </summary>
public sealed class DesignTimeInventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite("Data Source=ebay-hero.design.sqlite")
            .Options;

        return new InventoryDbContext(options);
    }
}

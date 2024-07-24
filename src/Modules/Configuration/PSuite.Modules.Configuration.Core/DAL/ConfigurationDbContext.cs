using Microsoft.EntityFrameworkCore;
using PSuite.Modules.Configuration.Core.Entities;

namespace PSuite.Modules.Configuration.Core.DAL;

internal sealed class ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<Room> Rooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("configuration");
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}

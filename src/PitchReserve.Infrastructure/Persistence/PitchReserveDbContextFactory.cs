using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PitchReserve.Infrastructure.Persistence;

/// <summary>Scaffolds schema without starting the API or connecting to Redis.</summary>
public sealed class PitchReserveDbContextFactory : IDesignTimeDbContextFactory<PitchReserveDbContext>
{
    public PitchReserveDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=PitchReserveDb;Integrated Security=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<PitchReserveDbContext>().UseSqlServer(connectionString).Options;
        return new PitchReserveDbContext(options);
    }
}

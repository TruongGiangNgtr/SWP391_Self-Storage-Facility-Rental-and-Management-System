using Microsoft.EntityFrameworkCore;

namespace Frms.DataAccess.Persistence;

/// <summary>Empty EF Core context for the FRMS scaffold.</summary>
public sealed class FrmsDbContext(DbContextOptions<FrmsDbContext> options)
    : DbContext(options);

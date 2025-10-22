using Microsoft.EntityFrameworkCore;

namespace pzellhorn.Core.State.Base.DBContext;

public partial class BaseDbContext(DbContextOptions<BaseDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        //grab & load our entities
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaseDbContext).Assembly);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

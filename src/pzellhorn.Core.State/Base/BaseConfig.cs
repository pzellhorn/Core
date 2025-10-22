using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using pzellhorn.Core.State.Base.Interfaces;

namespace pzellhorn.Core.State.Base
{
    //global settings for EFCore configuration
    public abstract class BaseConfig<T> : IEntityTypeConfiguration<T>
    where T : class, IIsDeleted, ICreatedAt, IModifiedAt
    {
        public virtual void Configure(EntityTypeBuilder<T> entity)
        {
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("now()");
        }
    }
}

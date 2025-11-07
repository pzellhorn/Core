using System.Linq.Expressions; 
using pzellhorn.Core.State.Base.DBContext;
using pzellhorn.Core.State.Base.Interfaces;

namespace pzellhorn.Core.State.Base
{
    public class BaseRepository<T>(BaseDbContext db) : IBaseRepository<T> where T : class, IIsDeleted, IPrimaryKeySelector<T>
    {
        public Task<T?> Get(Guid id, CancellationToken cancellationToken = default, bool excludeSoftDelete = true)
            => db.Get<T>(id, excludeSoftDelete, cancellationToken);

        public Task<List<T>> GetFor<TKey>(TKey key, Expression<Func<T, TKey>> property, CancellationToken cancellationToken = default, bool excludeSoftDelete = true)
            => db.GetFor(key, property, excludeSoftDelete, cancellationToken);

        public Task<List<T>> GetForMany<TKey>(IEnumerable<TKey> keys, Expression<Func<T, TKey>> property, CancellationToken cancellationToken = default, bool excludeSoftDelete = true)
           => db.GetForMany(keys, property, excludeSoftDelete, cancellationToken); 

        public Task<T> Upsert(T entity, CancellationToken cancellationToken = default)
            => db.Upsert(entity, cancellationToken);

        public Task<bool> Delete(Guid id, CancellationToken cancellationToken = default)
            => db.Delete<T>(id, cancellationToken);
    }
}

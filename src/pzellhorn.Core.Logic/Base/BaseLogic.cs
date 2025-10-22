using System.Linq.Expressions;
using pzellhorn.Core.State.Base.Interfaces;

namespace pzellhorn.Core.Logic.Base
{
    public class BaseLogic<T>(IBaseRepository<T> baseRepository) : IBaseLogic<T>
       where T : class, IIsDeleted, IPrimaryKeySelector<T>
    {
        public Task<T?> Get(Guid id, CancellationToken cancellationToken = default, bool excludeSoftDelete = true) => baseRepository.Get(id, cancellationToken, excludeSoftDelete);

        public async Task<T> Upsert(T entity, CancellationToken cancellationToken = default) => await baseRepository.Upsert(entity, cancellationToken);

        public async Task<bool> Delete(Guid id, CancellationToken cancellationToken = default)
            => await baseRepository.Delete(id, cancellationToken);

        public async Task<List<T>> GetFor<TKey>(TKey key, Expression<Func<T, TKey>> property, CancellationToken cancellationToken = default)
            => await baseRepository.GetFor(key, property, cancellationToken);
    }
}

using System.Linq.Expressions;

namespace pzellhorn.Core.State.Base.Interfaces
{
    public interface IBaseRepository<T> where T : class, IIsDeleted, IPrimaryKeySelector<T>
    {
        Task<T?> Get(Guid id, CancellationToken cancellationToken = default, bool excludeSoftDelete = true);
        Task<List<T>> GetFor<TKey>(TKey key, Expression<Func<T, TKey>> property, CancellationToken cancellationToken = default, bool excludeSoftDelete = true);
        Task<List<T>> GetForMany<TKey>(IEnumerable<TKey> keys, Expression<Func<T, TKey>> property, CancellationToken cancellationToken = default, bool excludeSoftDelete = true);
        Task<(List<T> Items, int TotalCount)> List(int page, int pageSize, CancellationToken cancellationToken = default, bool excludeSoftDelete = true);

        Task<T> Upsert(T entity, CancellationToken cancellationToken = default);
        Task<bool> Delete(Guid id, CancellationToken cancellationToken = default);

    }


}

using System.Linq.Expressions;
using pzellhorn.Core.State.Base.Interfaces;

namespace pzellhorn.Core.Logic.Base
{
    public interface IBaseLogic<T> where T : class, IIsDeleted, IPrimaryKeySelector<T>
    {
        Task<T?> Get(Guid id, CancellationToken cancellationToken = default, bool excludeSoftDelete = true);
        Task<List<T>> GetFor<TKey>(TKey key, Expression<Func<T, TKey>> property, CancellationToken cancellationToken = default);
        Task<T> Upsert(T entity, CancellationToken cancellationToken = default);
        Task<bool> Delete(Guid id, CancellationToken cancellationToken = default);
    }
}

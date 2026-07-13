using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using pzellhorn.Core.State.Base.Interfaces;

namespace pzellhorn.Core.State.Base
{

    // To include previously-deleted items in your result set, ensure the "excludeSoftDelete" flag is false.  
    // The Delete function below will mark an item isDeleted = true, rather than actually delete the item.
    // All calls to this class should go through BaseRepository or an overload of BaseRepository

    public static class BaseCrudLogic
    {
        //  gets & stores Primary Key for each model 
        private static class KeyCache<T> where T : class, IPrimaryKeySelector<T>
        {
            public static readonly Func<T, Guid> Get = T.PrimaryKey.Compile();
        }

        private sealed class ReplaceParamVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from, _to;
            public ReplaceParamVisitor(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }
            protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : base.VisitParameter(node);
        }

        private static Expression<Func<T, bool>> BuildKeyPredicate<T>(Guid id, bool includeDeleted)
           where T : class, IIsDeleted, IPrimaryKeySelector<T>
        {
            Expression<Func<T, Guid>> keySelection = T.PrimaryKey;
            ParameterExpression param = Expression.Parameter(typeof(T), "e");

            Expression key = new ReplaceParamVisitor(keySelection.Parameters[0], param).Visit(keySelection.Body)!;

            BinaryExpression expression = Expression.Equal(key, Expression.Constant(id));

            Expression body = includeDeleted ? expression :
                Expression.AndAlso(expression, Expression.Equal(Expression.Property(param, nameof(IIsDeleted.IsDeleted)), Expression.Constant(false)));

            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        public static Task<T?> Get<T>(this DbContext db,
            Guid id,
             bool excludeSoftDelete,
            CancellationToken cancellationToken = default)
            where T : class, IPrimaryKeySelector<T>, IIsDeleted
        {
            IQueryable<T> query = db.Set<T>().AsQueryable();
            if (!excludeSoftDelete)
                query = query.IgnoreQueryFilters();

            Expression<Func<T, bool>> pred = BuildKeyPredicate<T>(id, includeDeleted: !excludeSoftDelete);
            return query.SingleOrDefaultAsync(pred, cancellationToken);
        }

        public static Task<List<T>> GetForMany<T, TProp>(
         this DbContext db,
         IEnumerable<TProp> keys,
         Expression<Func<T, TProp>> property,
         bool excludeSoftDelete,
         CancellationToken cancellationToken = default)
         where T : class, IIsDeleted
        {
            ArgumentNullException.ThrowIfNull(keys);
            List<TProp> keyList = keys.Distinct().ToList();
            if (keyList.Count == 0) return Task.FromResult(new List<T>());

            IQueryable<T> query = db.Set<T>();
            if (!excludeSoftDelete) query = query.IgnoreQueryFilters();

            ParameterExpression param = property.Parameters[0];
            Expression propBody = property.Body;

            System.Reflection.MethodInfo containsMethod = typeof(Enumerable)
                .GetMethods()
                .Single(m => m.Name == nameof(Enumerable.Contains)
                          && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(TProp));

            ConstantExpression keysExpr = Expression.Constant(keyList, typeof(IEnumerable<TProp>));
            MethodCallExpression contains = Expression.Call(containsMethod, keysExpr, propBody);

            Expression body = excludeSoftDelete
                ? Expression.AndAlso(
                    contains,
                    Expression.Equal(
                        Expression.Property(param, nameof(IIsDeleted.IsDeleted)),
                        Expression.Constant(false)))
                : contains;

            Expression<Func<T, bool>> pred = Expression.Lambda<Func<T, bool>>(body, param);
            return query.Where(pred).ToListAsync(cancellationToken);
        }

        public static Task<List<T>> GetFor<T, TKey>(
          this DbContext db,
          TKey key,
          Expression<Func<T, TKey?>> property,
          bool excludeSoftDelete,
          CancellationToken cancellationToken = default)
          where T : class, IIsDeleted
        {
            IQueryable<T> query = db.Set<T>();
            if (!excludeSoftDelete) query = query.IgnoreQueryFilters();

            ParameterExpression param = property.Parameters[0];

            Type keyType = property.Body.Type;
            Type underlyingType = Nullable.GetUnderlyingType(keyType) ?? keyType;

            Expression rightConst = Expression.Constant(key, underlyingType);
            if (keyType != underlyingType)
                rightConst = Expression.Convert(rightConst, keyType);

            BinaryExpression equals = Expression.Equal(property.Body, rightConst);

            Expression body = excludeSoftDelete
                ? Expression.AndAlso(
                    equals,
                    Expression.Equal(
                        Expression.Property(param, nameof(IIsDeleted.IsDeleted)),
                        Expression.Constant(false)))
                : equals;

            Expression<Func<T, bool>> pred = Expression.Lambda<Func<T, bool>>(body, param);
            return query.Where(pred).ToListAsync(cancellationToken);
        }

        public static async Task<(List<T> Items, int TotalCount)> List<T>(
            this DbContext db,
            int page,
            int pageSize,
            bool excludeSoftDelete,
            CancellationToken cancellationToken = default)
            where T : class, IPrimaryKeySelector<T>, IIsDeleted
        {
            IQueryable<T> query = db.Set<T>();
            if (!excludeSoftDelete)
                query = query.IgnoreQueryFilters();
            else
                query = query.Where(e => !e.IsDeleted);

            int totalCount = await query.CountAsync(cancellationToken);

            IOrderedQueryable<T> ordered;
            if (typeof(ICreatedAt).IsAssignableFrom(typeof(T)))
            {
                ParameterExpression param = Expression.Parameter(typeof(T), "e");
                Expression<Func<T, DateTime>> createdAt = Expression.Lambda<Func<T, DateTime>>(
                    Expression.Property(param, nameof(ICreatedAt.CreatedAt)), param);
                ordered = query.OrderByDescending(createdAt).ThenBy(T.PrimaryKey);
            }
            else
            {
                ordered = query.OrderBy(T.PrimaryKey);
            }

            List<T> items = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public static async Task<T> Upsert<T>(
             this DbContext db,
             T incoming,
             CancellationToken cancellationToken = default)
             where T : class, IPrimaryKeySelector<T>, IIsDeleted
        {
            DbSet<T> set = db.Set<T>();
            Guid key = KeyCache<T>.Get(incoming);

            if (key == Guid.Empty)
            {
                await set.AddAsync(incoming, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return incoming;
            }

            Expression<Func<T, bool>> pred = BuildKeyPredicate<T>(key, includeDeleted: false);
            T? existingRecord = await set.IgnoreQueryFilters().SingleOrDefaultAsync(pred, cancellationToken);

            if (existingRecord is null)
            {
                //INSERT NEW
                await set.AddAsync(incoming, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return incoming;
            }
            else
            {
                // UPDATE
                EntityEntry<T> entry = db.Entry(existingRecord);
                entry.CurrentValues.SetValues(incoming);
                await db.SaveChangesAsync(cancellationToken);
                return existingRecord;
            }


        }

        //Not a real delete. sets isDeleted = true
        public static async Task<bool> Delete<T>(
            this DbContext db,
            Guid id,
            CancellationToken cancellationToken = default)
            where T : class, IPrimaryKeySelector<T>, IIsDeleted
        {
            DbSet<T> set = db.Set<T>();
            Expression<Func<T, bool>> pred = BuildKeyPredicate<T>(id, includeDeleted: true);
            T? entity = await set.IgnoreQueryFilters().SingleOrDefaultAsync(pred, cancellationToken);

            if (entity is null) return false;

            if (entity.IsDeleted) return true;

            entity.IsDeleted = true;

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

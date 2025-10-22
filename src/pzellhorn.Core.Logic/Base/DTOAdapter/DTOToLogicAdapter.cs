using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using pzellhorn.Core.State.Base.Interfaces;

namespace pzellhorn.Core.Logic.Base.DTOAdapter
{
    public class DtoLogicAdapter<TEntity, TReq, TRes>(
     IBaseLogic<TEntity> logic,
     IDTOMapper<TEntity, TReq, TRes> mapper)
     : IDtoLogicAdapter<TReq, TRes>
     where TEntity : class, IIsDeleted, IPrimaryKeySelector<TEntity>
    {
        public async Task<TRes?> Get(Guid id, CancellationToken cancellationToken = default)
                => await logic.Get(id, cancellationToken) is { } e ? mapper.ToResponse(e) : default;

        public async Task<TRes> Upsert(TReq request, CancellationToken cancellationToken = default)
        {
            TEntity entity;
            Guid? id = mapper.ExtractId(request);

            if (id.HasValue)    //create new or retrieve existing full object
            {
                entity = await logic.Get((Guid)id, cancellationToken) ?? throw new KeyNotFoundException($"Id {id} not found.");
                mapper.ApplyRequestToModel(request, entity);
            }
            else
            {
                entity = mapper.CreateEntity(request);
            }

            return mapper.ToResponse(await logic.Upsert(entity, cancellationToken));
        }

        public Task<bool> Delete(Guid id, CancellationToken cancellationToken = default) => logic.Delete(id, cancellationToken);

        public async Task<List<TRes>> GetFor(string key, string property, CancellationToken cancellationToken = default)
        {
            PropertyInfo prop = typeof(TEntity).GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                ?? throw new ArgumentException($"Property '{property}' not found on entity '{typeof(TEntity).Name}'.", nameof(property));

            ParameterExpression param = Expression.Parameter(typeof(TEntity), "e");
            Type type = typeof(Func<,>).MakeGenericType(typeof(TEntity), prop.PropertyType);
            LambdaExpression typedLambda = Expression.Lambda(type, Expression.Property(param, prop), param);

            MethodInfo getForMethod = typeof(IBaseLogic<TEntity>).GetMethod("GetFor")!;
            MethodInfo createdGetFor = getForMethod.MakeGenericMethod(prop.PropertyType);

            Task<List<TEntity>> task =
                (Task<List<TEntity>>)createdGetFor.Invoke(logic, new object[] { ConvertKeyString(key, prop.PropertyType), typedLambda, cancellationToken });

            List<TEntity> entities = await task;
            return entities.Select(mapper.ToResponse).ToList();
        }

        private static object ConvertKeyString(string key, Type targetType)
        {
            Type? underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                if (string.IsNullOrWhiteSpace(key)) return null;
                return ConvertKeyString(key, underlying);

            }
            if (targetType == typeof(string)) return key;
            if (targetType == typeof(Guid)) return Guid.Parse(key);
            if (targetType.IsEnum) return Enum.Parse(targetType, key, ignoreCase: true);

            if (targetType == typeof(int)) return int.Parse(key, CultureInfo.InvariantCulture);
            if (targetType == typeof(long)) return long.Parse(key, CultureInfo.InvariantCulture);
            if (targetType == typeof(short)) return short.Parse(key, CultureInfo.InvariantCulture);
            if (targetType == typeof(byte)) return byte.Parse(key, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return bool.Parse(key);
            if (targetType == typeof(decimal)) return decimal.Parse(key, CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(key, CultureInfo.InvariantCulture);
            if (targetType == typeof(float)) return float.Parse(key, CultureInfo.InvariantCulture);
            if (targetType == typeof(DateTime)) return DateTime.Parse(key, CultureInfo.InvariantCulture);

            return Convert.ChangeType(key, targetType, CultureInfo.InvariantCulture)!;
        }
    }
}

namespace pzellhorn.Core.ClientAPI.Base
{
    public interface IBaseClientApi<TReq, TRes>
    {
        Task<TRes?> Get(Guid id, CancellationToken cancellationToken = default);
        Task<List<TRes>> GetFor(string key, string propertyName, CancellationToken cancellationToken = default);
        Task<TRes> Upsert(TReq request, CancellationToken cancellationToken = default);
        Task<List<TRes>> UpsertMany(List<TReq> requests, CancellationToken cancellationToken = default);
        Task<bool> Delete(Guid id, CancellationToken cancellationToken = default);
    }
}

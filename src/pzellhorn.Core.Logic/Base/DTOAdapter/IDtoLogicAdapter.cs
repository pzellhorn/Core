namespace pzellhorn.Core.Logic.Base.DTOAdapter
{
    public interface IDtoLogicAdapter<TReq, TRes>
    {
        Task<TRes?> Get(Guid id, CancellationToken cancellationToken = default);
        Task<List<TRes>> GetFor(string key, string property, CancellationToken cancellationToken = default);
        Task<PagedResponse<TRes>> List(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<TRes> Upsert(TReq request, CancellationToken cancellationToken = default);
        Task<bool> Delete(Guid id, CancellationToken cancellationToken = default);
    }
}

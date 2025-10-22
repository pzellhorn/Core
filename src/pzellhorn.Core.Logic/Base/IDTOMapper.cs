namespace pzellhorn.Core.Logic.Base
{
    public interface IDTOMapper<TEntity, TReq, TRes>
    {
        Guid? ExtractId(TReq request);

        /// <summary>
        /// Applies Request object values to existing -model
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        void ApplyRequestToModel(TReq request, TEntity entity);

        /// <summary>
        /// Creates new Model entity based on request params
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        TEntity CreateEntity(TReq request);
        /// <summary>
        /// Converts Model to response object
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        TRes ToResponse(TEntity entity);
    }
}

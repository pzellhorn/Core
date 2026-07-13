using Microsoft.AspNetCore.Mvc;
using pzellhorn.Core.Logic.Base.DTOAdapter;
namespace pzellhorn.Core
{
    [ApiController]
    public abstract class BaseController<TReq, TRes>(
     IDtoLogicAdapter<TReq, TRes> logic) : ControllerBase
    {
        [HttpGet(nameof(Get))]
        public async Task<ActionResult<TRes>> Get(Guid id, CancellationToken cancellationToken)
            => (await logic.Get(id, cancellationToken)) is { } dto ? Ok(dto) : NotFound();

        /// <summary>
        /// Call using property name. Property name should be in PascalCase (eg: "RedPandaFoodItem")
        /// </summary>
        /// <param name="key"></param>
        /// <param name="propertyName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetFor))]
        public async Task<ActionResult<List<TRes>>> GetFor(string key, string propertyName, CancellationToken cancellationToken = default)
            => Ok(await logic.GetFor(key, propertyName, cancellationToken));

        [HttpGet(nameof(List))]
        public async Task<ActionResult<PagedResponse<TRes>>> List(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            return Ok(await logic.List(page, pageSize, cancellationToken));
        }

        [HttpPost(nameof(Upsert))]
        public async Task<ActionResult<TRes>> Upsert([FromBody] TReq request, CancellationToken cancellationToken)
            => Ok(await logic.Upsert(request, cancellationToken));

        [HttpPost(nameof(UpsertMany))]
        public async Task<ActionResult<List<TRes>>> UpsertMany([FromBody] List<TReq> requests, CancellationToken cancellationToken)
        {
            List<TRes> responses = new();
            foreach (TReq request in requests)
                responses.Add(await logic.Upsert(request, cancellationToken));
            return Ok(responses);
        }

        [HttpDelete(nameof(Delete))]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
            => await logic.Delete(id, cancellationToken) ? NoContent() : NotFound();
    }
}
 
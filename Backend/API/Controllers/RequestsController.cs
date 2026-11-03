using Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using Services.IServices;

namespace API.Controllers
{
    [ApiController]
    [Route("api/requests")]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestService _service;
        public RequestsController(IRequestService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<PagedResult<RequestDto>>> GetAll(
            [FromQuery] RequestQuery query, CancellationToken ct)
            => Ok(await _service.GetPagedAsync(query, ct));
    }
}

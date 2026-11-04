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

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<RequestDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequest body, CancellationToken ct)
        {
            var result = await _service.UpdateStatusAsync(id, body, ct);

            switch (result.Outcome)
            {
                case UpdateOutcome.Success:
                    return Ok(result.Request);
                case UpdateOutcome.NotFound:
                    return Problem(title: "Request not found", detail: result.Error,
                                   statusCode: StatusCodes.Status404NotFound);
                case UpdateOutcome.Conflict:
                    return Problem(title: "Concurrency conflict", detail: result.Error,
                                   statusCode: StatusCodes.Status409Conflict);
                default:
                    return Problem(title: "Invalid status transition", detail: result.Error,
                                   statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<IReadOnlyList<RequestStatusHistoryDto>>> GetHistory( int id, CancellationToken ct)
        {
            var history = await _service.GetHistoryAsync(id, ct);

            return history is null
                ? Problem(title: "Request not found", detail: $"Request {id} was not found.",
                          statusCode: StatusCodes.Status404NotFound)
                : Ok(history);
        }

        [HttpPost("bulk-status")]
        public async Task<ActionResult<BulkUpdateResult>> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest body, CancellationToken ct)
            => Ok(await _service.BulkUpdateStatusAsync(body, ct));

        [HttpGet("summary")]
        public async Task<ActionResult<RequestSummaryDto>> GetSummary(CancellationToken ct)
            => Ok(await _service.GetSummaryAsync(ct));

        [HttpGet("assignees")]
        public async Task<ActionResult<IReadOnlyList<string>>> GetAssignees(CancellationToken ct)
                => Ok(await _service.GetAssigneesAsync(ct));
    }
}

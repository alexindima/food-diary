using FoodDiary.Modules.Images.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;

using FoodDiary.Modules.Images.Presentation.Requests;
using FoodDiary.Modules.Images.Presentation.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Images.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/images")]
public sealed class ImagesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("upload-url")]
    [EnableIdempotency]
    [ProducesResponseType<GetImageUploadUrlHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.ImagesRateLimitPolicyName)]
    public Task<IActionResult> GetUploadUrl([FromCurrentUser] Guid userId, [FromBody] GetImageUploadUrlHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPost("{assetId:guid}/confirm")]
    [EnableIdempotency]
    [ProducesResponseType<ConfirmImageUploadHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.ImagesRateLimitPolicyName)]
    public Task<IActionResult> Confirm(Guid assetId, [FromCurrentUser] Guid userId) =>
        HandleOk(assetId.ToConfirmCommand(userId), static value => value.ToHttpResponse());

    [HttpDelete("{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> Delete(Guid assetId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(assetId.ToDeleteCommand(userId));
}

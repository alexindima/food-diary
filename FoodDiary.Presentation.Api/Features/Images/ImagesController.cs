using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Images.Mappings;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Images.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Images;

[ApiController]
[Route("api/v{version:apiVersion}/images")]
public sealed class ImagesController(ISender mediator, ILogger<ImagesController> logger) : AuthorizedController(mediator) {
    [HttpPost("upload-url")]
    [EnableIdempotency]
    [ProducesResponseType<GetImageUploadUrlHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> GetUploadUrl([FromCurrentUser] Guid userId, [FromBody] GetImageUploadUrlHttpRequest request) =>
        HandleObservedOk(request.ToCommand(userId), static value => value.ToHttpResponse(), logger, "images.upload-url", userId);

    [HttpDelete("{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> Delete(Guid assetId, [FromCurrentUser] Guid userId) =>
        HandleObservedNoContent(assetId.ToDeleteCommand(userId), logger, "images.delete", userId);
}

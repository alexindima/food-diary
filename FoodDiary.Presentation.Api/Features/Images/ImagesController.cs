using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Images.Mappings;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Images.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Images;

[ApiController]
[Route("api/images")]
public sealed class ImagesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("upload-url")]
    [ProducesResponseType<GetImageUploadUrlHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> GetUploadUrl([FromCurrentUser] Guid userId, [FromBody] GetImageUploadUrlHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpDelete("{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> Delete(Guid assetId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(assetId.ToDeleteCommand(userId));
}


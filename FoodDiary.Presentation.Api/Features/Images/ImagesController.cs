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
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetUploadUrl([FromCurrentUser] Guid userId, [FromBody] GetImageUploadUrlHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpDelete("{assetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Delete(Guid assetId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(assetId.ToDeleteCommand(userId));
}


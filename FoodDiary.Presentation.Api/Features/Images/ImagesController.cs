using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Images.Mappings;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Images.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Images;

[ApiController]
[Route("api/[controller]")]
public sealed class ImagesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("upload-url")]
    public async Task<IActionResult> GetUploadUrl([FromCurrentUser] Guid userId, [FromBody] GetImageUploadUrlHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => new GetImageUploadUrlHttpResponse(
            value.UploadUrl,
            value.FileUrl,
            value.ObjectKey,
            value.ExpiresAtUtc,
            value.AssetId));
    }

    [HttpDelete("{assetId:guid}")]
    public async Task<IActionResult> Delete(Guid assetId, [FromCurrentUser] Guid userId) {
        var command = assetId.ToDeleteCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToNoContentActionResult();
    }
}

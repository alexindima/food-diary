using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
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
    public async Task<ActionResult<GetImageUploadUrlHttpResponse>> GetUploadUrl([FromCurrentUser] UserId userId, [FromBody] GetImageUploadUrlHttpRequest request) {
        var command = request.ToCommand(userId);

        GetImageUploadUrlResult result;
        try {
            result = await Mediator.Send(command);
        } catch (ArgumentException ex) {
            return BadRequest(ex.Message);
        } catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }

        var response = new GetImageUploadUrlHttpResponse(
            result.UploadUrl,
            result.FileUrl,
            result.ObjectKey,
            result.ExpiresAtUtc,
            result.AssetId);

        return Ok(response);
    }

    [HttpDelete("{assetId:guid}")]
    public async Task<IActionResult> Delete(Guid assetId, [FromCurrentUser] UserId userId) {
        var command = assetId.ToDeleteCommand(userId);
        var result = await Mediator.Send(command);

        if (!result.Deleted) {
            return result.ErrorCode switch {
                "not_found" => NotFound(),
                "forbidden" => Forbid(),
                "in_use" => Conflict("Asset is already in use."),
                "storage_error" => StatusCode(502, "Failed to remove image from storage."),
                _ => BadRequest()
            };
        }

        return NoContent();
    }
}

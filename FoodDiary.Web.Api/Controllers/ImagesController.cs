using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Application.Images.Commands.DeleteImageAsset;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Contracts.Images;
using FoodDiary.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ImagesController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpPost("upload-url")]
    public async Task<ActionResult<GetImageUploadUrlResponse>> GetUploadUrl([FromBody] GetImageUploadUrlRequest request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var command = new GetImageUploadUrlCommand(
            CurrentUserId.Value,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes);

        GetImageUploadUrlResult result;
        try
        {
            result = await Mediator.Send(command);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        var response = new GetImageUploadUrlResponse(
            result.UploadUrl,
            result.FileUrl,
            result.ObjectKey,
            result.ExpiresAtUtc,
            result.AssetId);

        return Ok(response);
    }

    [HttpDelete("{assetId:guid}")]
    public async Task<IActionResult> Delete(Guid assetId)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var command = new DeleteImageAssetCommand(CurrentUserId.Value, new(assetId));
        var result = await Mediator.Send(command);

        if (!result.Deleted)
        {
            return result.ErrorCode switch
            {
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

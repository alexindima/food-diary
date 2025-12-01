using FoodDiary.Application.Images.Commands.GetUploadUrl;
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
}

using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Version.Responses;
using FoodDiary.Presentation.Api.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Version;

[ApiController]
[Route("api")]
[ApiExplorerSettings(IgnoreApi = true)]
[SuppressRequestAccessLog]
public sealed class VersionController(ISender mediator, IApiVersionInfo versionInfo) : BaseApiController(mediator) {
    [HttpGet("version")]
    [HttpGet("v1/version")]
    public IActionResult GetVersion() =>
        Ok(new ApiVersionHttpResponse(
            versionInfo.CommitSha,
            versionInfo.ImageTag,
            versionInfo.Environment,
            versionInfo.ApplicationVersion,
            versionInfo.StartedAtUtc));
}

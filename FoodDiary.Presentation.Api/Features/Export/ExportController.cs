using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Export.Mappings;
using FoodDiary.Presentation.Api.Features.Export.Requests;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Export;

[ApiController]
[Route("api/v{version:apiVersion}/export")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ExportController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("diary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.ExportRateLimitPolicyName)]
    public Task<IActionResult> ExportDiary(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery, MaxLength(ExportRequestLimits.MaximumFormatLength), RegularExpression("^(?i:csv|pdf)$", ErrorMessage = "Format must be csv or pdf.")] string format = "csv",
        [FromQuery, MaxLength(ExportRequestLimits.MaximumLocaleLength)] string? locale = null,
        [FromQuery, Range(ExportRequestLimits.MinimumTimeZoneOffsetMinutes, ExportRequestLimits.MaximumTimeZoneOffsetMinutes)] int? timeZoneOffsetMinutes = null,
        [FromQuery, MaxLength(ExportRequestLimits.MaximumReportOriginLength)] string? reportOrigin = null) =>
        HandleFile(ExportHttpMappings.ToQuery(userId, dateFrom, dateTo, format, locale, timeZoneOffsetMinutes, reportOrigin));

    [HttpGet("cycle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.ExportRateLimitPolicyName)]
    public Task<IActionResult> ExportCycle(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery, Range(ExportRequestLimits.MinimumTimeZoneOffsetMinutes, ExportRequestLimits.MaximumTimeZoneOffsetMinutes)] int? timeZoneOffsetMinutes = null) =>
        HandleFile(ExportHttpMappings.ToCycleQuery(userId, dateFrom, dateTo, timeZoneOffsetMinutes));

    [HttpPost("cycle/sensitive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.SecretVerificationRateLimitPolicyName)]
    public Task<IActionResult> ExportSensitiveCycle(
        [FromCurrentUser] Guid userId,
        [FromBody] SensitiveCycleExportHttpRequest request) =>
        HandleFile(ExportHttpMappings.ToSensitiveCycleQuery(
            userId,
            request.DateFrom,
            request.DateTo,
            request.CurrentPassword,
            request.TimeZoneOffsetMinutes));
}

using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Presentation.Recipes.Mappings;
using FoodDiary.Modules.Ai.Presentation.Recipes.Requests;
using FoodDiary.Modules.Ai.Presentation.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Ai.Presentation.Recipes.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/ai/food/recipe-import")]
[Authorize(Roles = PresentationRoleNames.Premium)]
[EnableRateLimiting(PresentationPolicyNames.AiRateLimitPolicyName)]
[RequestSizeLimit(52 * 1024 * 1024)]
[RejectOversizedRequest(52 * 1024 * 1024)]
[RecipeVideoAdmission]
[ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
public sealed class RecipeVideoImportController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("video")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024, ValueLengthLimit = 16000)]
    [EnableIdempotency(requireKey: true)]
    [ProducesResponseType<RecipeImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> ImportRecipeVideo([FromCurrentUser] Guid userId, IFormFile? video, [FromForm] string? sourceUrl, [FromForm] string? text) =>
        HandleVideoImportAsync(userId, video, new RecipeVideoImportHttpRequest(sourceUrl, text), HttpContext.RequestAborted);

    private async Task<IActionResult> HandleVideoImportAsync(Guid userId, IFormFile? video, RecipeVideoImportHttpRequest request, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = video?.OpenReadStream() ?? Stream.Null;
        await using (stream.ConfigureAwait(false)) {
            string requestId = IdempotencyRequestContext.GetRequestId(HttpContext) ?? throw new InvalidOperationException("Required idempotency context is unavailable.");
            return await HandleOk(request.ToCommand(userId, video is null ? null : stream, requestId), static draft => draft.ToHttpResponse()).ConfigureAwait(false);
        }
    }
}

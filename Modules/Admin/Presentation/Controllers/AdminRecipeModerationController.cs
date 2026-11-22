using Asp.Versioning;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/moderation")]
[Authorize(Roles = PresentationRoleNames.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminRecipeModerationController(IRecipeModerationAdministration administration) : ControllerBase {
    [HttpGet("recipes")]
    [ProducesResponseType<RecipeModerationPage>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecipes([FromQuery] GetAdminRecipeModerationHttpQuery query) =>
        Ok(await administration.ListAsync(query.CanonicalStatus(), query.Page, query.Limit, HttpContext.RequestAborted));

    [HttpGet("recipes/{id:guid}")]
    [ProducesResponseType<RecipeModerationItem>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecipe(Guid id) {
        RecipeModerationItem? item = await administration.GetAsync(id, HttpContext.RequestAborted);
        return item is null ? RevisionNotFound() : Ok(item);
    }

    [HttpGet("recipes/{id:guid}/images/{assetId:guid}")]
    [ProducesResponseType<AdminRecipeModerationImageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(Guid id, Guid assetId) {
        string? dataUrl = await administration.GetImageAsync(id, assetId, HttpContext.RequestAborted);
        return dataUrl is null
            ? Error(StatusCodes.Status404NotFound, "RecipeModeration.ImageNotFound", "The checked image is no longer available.")
            : Ok(new AdminRecipeModerationImageHttpResponse(dataUrl));
    }

    [HttpPost("recipes/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveRecipe(
        Guid id,
        [FromCurrentUser] Guid actorId,
        [FromBody] AdminRecipeModerationResolveHttpRequest request) {
        bool approve = request.Approve == true;
        string? reason = request.Reason?.Trim();
        if (!approve && string.IsNullOrWhiteSpace(reason)) {
            return Error(StatusCodes.Status400BadRequest, "RecipeModeration.ReasonRequired", "Provide a reason for requesting changes.");
        }

        RecipeModerationActionOutcome result = await administration.ResolveAsync(id, actorId, approve, reason, HttpContext.RequestAborted);
        return ActionResult(result);
    }

    [HttpPost("recipes/{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryRecipe(Guid id, [FromCurrentUser] Guid actorId) {
        RecipeModerationActionOutcome result = await administration.RetryAsync(id, actorId, HttpContext.RequestAborted);
        return ActionResult(result);
    }

    [HttpGet("rules")]
    [ProducesResponseType<RecipeModerationPolicyView>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules() =>
        Ok(await administration.GetPolicyAsync(HttpContext.RequestAborted));

    [HttpPut("rules")]
    [ProducesResponseType<RecipeModerationPolicyView>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishRules([FromCurrentUser] Guid actorId, [FromBody] AdminRecipeModerationRulesHttpRequest request) {
        if (request.TextReviewCategories is null || request.ImageReviewCategories is null || !request.MistralSamplePercent.HasValue) {
            return InvalidRules();
        }

        var policy = new RecipeModerationPolicyInput(request.TextReviewCategories, request.ImageReviewCategories, request.MistralSamplePercent.Value);
        try {
            return Ok(await administration.PublishPolicyAsync(actorId, policy, HttpContext.RequestAborted));
        } catch (ArgumentException) {
            return InvalidRules();
        }
    }

    [HttpPost("rules/{id:guid}/restore")]
    [ProducesResponseType<RecipeModerationPolicyView>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreRules(Guid id, [FromCurrentUser] Guid actorId) {
        RecipeModerationPolicyView? policy = await administration.RestorePolicyAsync(id, actorId, HttpContext.RequestAborted);
        return policy is null
            ? Error(StatusCodes.Status404NotFound, "RecipeModeration.PolicyNotFound", "The rule version no longer exists.")
            : Ok(policy);
    }

    private IActionResult ActionResult(RecipeModerationActionOutcome outcome) =>
        outcome switch {
            RecipeModerationActionOutcome.Applied => NoContent(),
            RecipeModerationActionOutcome.NotFound => RevisionNotFound(),
            RecipeModerationActionOutcome.Conflict => Error(
                StatusCodes.Status409Conflict, "RecipeModeration.Conflict", "The submission has changed. Reload it before trying again."),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };

    private IActionResult RevisionNotFound() =>
        Error(StatusCodes.Status404NotFound, "RecipeModeration.RevisionNotFound", "The submission no longer exists.");

    private IActionResult InvalidRules() =>
        Error(StatusCodes.Status400BadRequest, "RecipeModeration.InvalidRules", "Select supported categories and a sample percentage from 0 to 100.");

    private ObjectResult Error(int statusCode, string code, string message) =>
        StatusCode(statusCode, new ApiErrorHttpResponse(code, message, HttpContext.TraceIdentifier));
}

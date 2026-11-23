using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Mappings;

public sealed class AdminRecipeModerationHttpProcessor(IRecipeModerationAdministration administration) {
    public async Task<IActionResult> ListAsync(GetAdminRecipeModerationHttpQuery query, HttpContext context) =>
        new OkObjectResult(await administration.ListAsync(query.CanonicalStatus(), query.Page, query.Limit, context.RequestAborted));

    public async Task<IActionResult> GetAsync(Guid id, HttpContext context) {
        RecipeModerationItem? item = await administration.GetAsync(id, context.RequestAborted);
        return item is null ? RevisionNotFound(context) : new OkObjectResult(item);
    }

    public async Task<IActionResult> GetImageAsync(Guid id, Guid assetId, HttpContext context) {
        string? dataUrl = await administration.GetImageAsync(id, assetId, context.RequestAborted);
        return dataUrl is null
            ? Error(context, StatusCodes.Status404NotFound, "RecipeModeration.ImageNotFound", "The checked image is no longer available.")
            : new OkObjectResult(new AdminRecipeModerationImageHttpResponse(dataUrl));
    }

    public async Task<IActionResult> ResolveAsync(
        Guid id,
        Guid actorId,
        AdminRecipeModerationResolveHttpRequest request,
        HttpContext context) {
        bool approve = request.Approve == true;
        string? reason = request.Reason?.Trim();
        if (!approve && string.IsNullOrWhiteSpace(reason)) {
            return Error(context, StatusCodes.Status400BadRequest, "RecipeModeration.ReasonRequired", "Provide a reason for requesting changes.");
        }

        RecipeModerationActionOutcome result = await administration.ResolveAsync(id, actorId, approve, reason, context.RequestAborted);
        return ActionResult(result, context);
    }

    public async Task<IActionResult> RetryAsync(Guid id, Guid actorId, HttpContext context) {
        RecipeModerationActionOutcome result = await administration.RetryAsync(id, actorId, context.RequestAborted);
        return ActionResult(result, context);
    }

    public async Task<IActionResult> GetRulesAsync(HttpContext context) =>
        new OkObjectResult(await administration.GetPolicyAsync(context.RequestAborted));

    public async Task<IActionResult> PublishRulesAsync(Guid actorId, AdminRecipeModerationRulesHttpRequest request, HttpContext context) {
        if (request.TextReviewCategories is null || request.ImageReviewCategories is null || !request.MistralSamplePercent.HasValue) {
            return InvalidRules(context);
        }

        var policy = new RecipeModerationPolicyInput(request.TextReviewCategories, request.ImageReviewCategories, request.MistralSamplePercent.Value);
        try {
            return new OkObjectResult(await administration.PublishPolicyAsync(actorId, policy, context.RequestAborted));
        } catch (ArgumentException) {
            return InvalidRules(context);
        }
    }

    public async Task<IActionResult> RestoreRulesAsync(Guid id, Guid actorId, HttpContext context) {
        RecipeModerationPolicyView? policy = await administration.RestorePolicyAsync(id, actorId, context.RequestAborted);
        return policy is null
            ? Error(context, StatusCodes.Status404NotFound, "RecipeModeration.PolicyNotFound", "The rule version no longer exists.")
            : new OkObjectResult(policy);
    }

    private static IActionResult ActionResult(RecipeModerationActionOutcome outcome, HttpContext context) =>
        outcome switch {
            RecipeModerationActionOutcome.Applied => new NoContentResult(),
            RecipeModerationActionOutcome.NotFound => RevisionNotFound(context),
            RecipeModerationActionOutcome.Conflict => Error(
                context, StatusCodes.Status409Conflict, "RecipeModeration.Conflict", "The submission has changed. Reload it before trying again."),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };

    private static ObjectResult RevisionNotFound(HttpContext context) =>
        Error(context, StatusCodes.Status404NotFound, "RecipeModeration.RevisionNotFound", "The submission no longer exists.");

    private static ObjectResult InvalidRules(HttpContext context) =>
        Error(context, StatusCodes.Status400BadRequest, "RecipeModeration.InvalidRules", "Select supported categories and a sample percentage from 0 to 100.");

    private static ObjectResult Error(HttpContext context, int statusCode, string code, string message) =>
        new(new ApiErrorHttpResponse(code, message, context.TraceIdentifier)) { StatusCode = statusCode };
}

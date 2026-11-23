using Asp.Versioning;
using FoodDiary.Modules.Admin.Presentation.Mappings;
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
public sealed class AdminRecipeModerationController(AdminRecipeModerationHttpProcessor moderation) : ControllerBase {
    [HttpGet("recipes")]
    [ProducesResponseType<RecipeModerationPage>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetRecipes([FromQuery] GetAdminRecipeModerationHttpQuery query) =>
        moderation.ListAsync(query, HttpContext);

    [HttpGet("recipes/{id:guid}")]
    [ProducesResponseType<RecipeModerationItem>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetRecipe(Guid id) =>
        moderation.GetAsync(id, HttpContext);

    [HttpGet("recipes/{id:guid}/images/{assetId:guid}")]
    [ProducesResponseType<AdminRecipeModerationImageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetImage(Guid id, Guid assetId) =>
        moderation.GetImageAsync(id, assetId, HttpContext);

    [HttpPost("recipes/{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> ResolveRecipe(
        Guid id,
        [FromCurrentUser] Guid actorId,
        [FromBody] AdminRecipeModerationResolveHttpRequest request) =>
        moderation.ResolveAsync(id, actorId, request, HttpContext);

    [HttpPost("recipes/{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> RetryRecipe(Guid id, [FromCurrentUser] Guid actorId) =>
        moderation.RetryAsync(id, actorId, HttpContext);

    [HttpGet("rules")]
    [ProducesResponseType<RecipeModerationPolicyView>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetRules() =>
        moderation.GetRulesAsync(HttpContext);

    [HttpPut("rules")]
    [ProducesResponseType<RecipeModerationPolicyView>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> PublishRules([FromCurrentUser] Guid actorId, [FromBody] AdminRecipeModerationRulesHttpRequest request) =>
        moderation.PublishRulesAsync(actorId, request, HttpContext);

    [HttpPost("rules/{id:guid}/restore")]
    [ProducesResponseType<RecipeModerationPolicyView>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> RestoreRules(Guid id, [FromCurrentUser] Guid actorId) =>
        moderation.RestoreRulesAsync(id, actorId, HttpContext);
}

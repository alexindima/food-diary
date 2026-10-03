using FoodDiary.Mediator;
using FoodDiary.Modules.Recipes.Presentation.Mappings;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Modules.Recipes.Presentation.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Recipes.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/catalog/recipes")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminCatalogRecipesController(ISender sender) : BaseApiController(sender) {
    [HttpGet("export")]
    [ProducesResponseType<List<CatalogRecipeExportHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Export() =>
        HandleOk(CatalogRecipeHttpMappings.ToExportQuery(), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpPost("preview")]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<CatalogRecipeImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Preview([FromCurrentUser] Guid userId, [FromBody] CatalogRecipeHttpRequest recipe) =>
        HandleOk(recipe.ToImportCommand(userId, preview: true), static value => value.ToHttpResponse());

    [HttpPost("import")]
    [EnableIdempotency(requireKey: true)]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<CatalogRecipeImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Import([FromCurrentUser] Guid userId, [FromBody] CatalogRecipeHttpRequest recipe) =>
        HandleOk(recipe.ToImportCommand(userId, preview: false), static value => value.ToHttpResponse());
}

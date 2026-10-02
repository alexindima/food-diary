using FoodDiary.Mediator;
using FoodDiary.Modules.Recipes.Application.Commands.ImportCatalogRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Application.Queries.ExportCatalogRecipes;
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
    [ProducesResponseType<List<CatalogRecipeModel>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Export() => HandleOk(new ExportCatalogRecipesQuery(), static value => value);

    [HttpPost("preview")]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<CatalogRecipeImportResult>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Preview([FromCurrentUser] Guid userId, [FromBody] CatalogRecipeModel recipe) =>
        HandleOk(new ImportCatalogRecipeCommand(userId, recipe, Preview: true), static value => value);

    [HttpPost("import")]
    [EnableIdempotency(requireKey: true)]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<CatalogRecipeImportResult>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Import([FromCurrentUser] Guid userId, [FromBody] CatalogRecipeModel recipe) =>
        HandleOk(new ImportCatalogRecipeCommand(userId, recipe, Preview: false), static value => value);
}

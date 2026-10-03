using FoodDiary.Mediator;
using FoodDiary.Modules.Products.Presentation.Mappings;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Products.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/catalog/products")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminCatalogProductsController(ISender sender) : BaseApiController(sender) {
    [HttpGet("export")]
    [ProducesResponseType<List<CatalogProductHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Export() =>
        HandleOk(CatalogProductHttpMappings.ToExportQuery(), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpPost("preview")]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<CatalogProductImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Preview([FromCurrentUser] Guid userId, [FromBody] CatalogProductHttpRequest product) =>
        HandleOk(product.ToImportCommand(userId, preview: true), static value => value.ToHttpResponse());

    [HttpPost("import")]
    [EnableIdempotency(requireKey: true)]
    [RequestSizeLimit(PresentationRequestLimits.AdminImportPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AdminImportPayloadBytes)]
    [ProducesResponseType<CatalogProductImportHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> Import([FromCurrentUser] Guid userId, [FromBody] CatalogProductHttpRequest product) =>
        HandleOk(product.ToImportCommand(userId, preview: false), static value => value.ToHttpResponse());
}

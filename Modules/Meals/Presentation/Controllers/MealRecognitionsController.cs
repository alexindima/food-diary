using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.Meals.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Meals.Presentation.Requests;
using FoodDiary.Modules.Meals.Presentation.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Meals.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/meals")]
public sealed class MealRecognitionsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("recognitions/{operationId:guid}/undo")]
    [ProducesResponseType<RecognizedMealUndoHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> UndoRecognition(Guid operationId, [FromCurrentUser] Guid userId) =>
        HandleOk(operationId.ToUndoRecognizedMealCommand(userId), static value => value.ToHttpResponse());

    [HttpPost("recognitions/{recognitionId:guid}")]
    [RequestSizeLimit(PresentationRequestLimits.RichWritePayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.RichWritePayloadBytes)]
    [ProducesResponseType<RecognizedMealCreationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    public Task<IActionResult> CreateFromRecognition(Guid recognitionId, [FromCurrentUser] Guid userId,
        [FromBody] CreateMealFromRecognitionHttpRequest request) =>
        HandleOk(request.ToCommand(userId, recognitionId), static value => value.ToHttpResponse());
}

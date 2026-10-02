using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.Dietologist.Presentation.Mappings;
using FoodDiary.Modules.Dietologist.Presentation.Responses;
using FoodDiary.Modules.Dietologist.Presentation.Requests;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Requests;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Dietologist.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/recommendations")]
public sealed class RecommendationsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<RecommendationHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMyRecommendations(
        [FromCurrentUser] Guid userId,
        [FromQuery] GetRecommendationsHttpQuery? query = null) =>
        HandleOk(userId.ToMyRecommendationsQuery(query?.Page ?? 1, query?.Limit ?? 50), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpPut("{recommendationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkAsRead(Guid recommendationId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(recommendationId.ToMarkReadCommand(userId));

    [HttpGet("{recommendationId:guid}/comments")]
    [ProducesResponseType<PagedHttpResponse<RecommendationCommentHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetComments(
        Guid recommendationId,
        [FromCurrentUser] Guid userId,
        [FromQuery] OffsetPaginationHttpQuery pagination) =>
        HandleOk(
            recommendationId.ToRecommendationCommentsQuery(userId, pagination.Page, pagination.Limit),
            static value => new PagedHttpResponse<RecommendationCommentHttpResponse>(
                value.Data.Select(comment => comment.ToHttpResponse()).ToArray(), value.Page, value.Limit, value.TotalPages, value.TotalItems));

    [HttpPost("{recommendationId:guid}/comments")]
    [EnableIdempotency]
    [ProducesResponseType<RecommendationCommentHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> CreateComment(
        Guid recommendationId,
        [FromCurrentUser] Guid userId,
        [FromBody] CreateRecommendationCommentHttpRequest request) =>
        HandleCreated(
            request.ToCommand(userId, recommendationId),
            static value => value.ToHttpResponse());
}

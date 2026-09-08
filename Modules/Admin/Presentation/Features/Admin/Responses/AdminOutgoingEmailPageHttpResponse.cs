namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminOutgoingEmailPageHttpResponse(IReadOnlyList<AdminOutgoingEmailHttpResponse> Items, long TotalItems, IReadOnlyDictionary<string, long>? StatusCounts = null);

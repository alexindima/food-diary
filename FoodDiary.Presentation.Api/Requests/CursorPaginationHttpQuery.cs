using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Requests;

public sealed record CursorPaginationHttpQuery(
    [MaxLength(PresentationQueryLimits.MaximumCursorLength)] string? Cursor = null,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 10);

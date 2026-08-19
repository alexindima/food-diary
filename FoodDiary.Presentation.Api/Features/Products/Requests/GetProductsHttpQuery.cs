using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Products.Requests;

public sealed record GetProductsHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 10,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    bool IncludePublic = true,
    [MaxLength(PresentationQueryLimits.MaximumCsvFilterLength)] string? ProductTypes = null,
    [OpenApiNumericRange(0)] double? CaloriesFrom = null,
    [OpenApiNumericRange(0)] double? CaloriesTo = null,
    bool? HasImage = null);

using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Products.Requests;

public sealed record GetProductsHttpQuery(
    int Page = 1,
    int Limit = 10,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    bool IncludePublic = true,
    [MaxLength(PresentationQueryLimits.MaximumCsvFilterLength)] string? ProductTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null);

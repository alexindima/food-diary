using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminRecipeModerationRulesHttpRequest(
    [Required] [MinLength(1)] IReadOnlyList<string>? TextReviewCategories,
    [Required] [MinLength(1)] IReadOnlyList<string>? ImageReviewCategories,
    [Required] [Range(0, 100)] int? MistralSamplePercent);

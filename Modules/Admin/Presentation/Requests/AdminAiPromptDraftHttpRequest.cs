using System.ComponentModel.DataAnnotations;
namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminAiPromptDraftHttpRequest(
    [Required, MaxLength(64)] string Key, [Required, RegularExpression("^(en|ru)$")] string Locale,
    [Required, MaxLength(4096)] string PromptText, [MaxLength(2048)] string? Text,
    Guid? ImageAssetId, [MaxLength(256)] string? FoodName, decimal? Amount, [MaxLength(32)] string? Unit);

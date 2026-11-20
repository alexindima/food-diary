using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminRecipeModerationResolveHttpRequest(
    [Required] bool? Approve,
    [MaxLength(2000)] string? Reason);

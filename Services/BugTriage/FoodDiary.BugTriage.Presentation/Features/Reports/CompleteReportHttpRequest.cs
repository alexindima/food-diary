using System.ComponentModel.DataAnnotations;

namespace FoodDiary.BugTriage.Presentation.Features.Reports;

public sealed record CompleteReportHttpRequest(Guid LeaseToken,
    [Required, StringLength(40)] string Outcome,
    [Required, StringLength(8000)] string Summary,
    [StringLength(2048)] string? MergeRequestUrl);

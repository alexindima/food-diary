using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminRecipeModerationHttpQuery(
    [AllowedQueryValues("Pending", "NeedsReview", "ChangesRequested", "CheckDelayed", "Published", "Superseded")] string? Status = null,
    [Range(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [Range(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 20) {
    private static readonly string[] StatusValues = ["Pending", "NeedsReview", "ChangesRequested", "CheckDelayed", "Published", "Superseded"];

    internal string? CanonicalStatus() =>
        Array.Find(StatusValues, value => string.Equals(value, Status, StringComparison.OrdinalIgnoreCase));
}

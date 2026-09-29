using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminMailInboxMessagePageHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 50,
    [MaxLength(320)] string? Recipient = null,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)] string? Category = null,
    bool? Unread = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null,
    [MaxLength(320)] string? Search = null, [MaxLength(320)] string? FromAddress = null, Guid? Id = null);

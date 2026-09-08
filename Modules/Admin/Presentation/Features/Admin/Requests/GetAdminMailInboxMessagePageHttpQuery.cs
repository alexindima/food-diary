using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminMailInboxMessagePageHttpQuery(
    [OpenApiNumericRange(1, int.MaxValue)] int Page = 1,
    [OpenApiNumericRange(1, 200)] int Limit = 50,
    [MaxLength(320)] string? Recipient = null,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)] string? Category = null,
    bool? Unread = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null,
    [MaxLength(320)] string? Search = null, [MaxLength(320)] string? FromAddress = null, Guid? Id = null);

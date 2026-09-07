using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminMailInboxMessagesHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumAdminMailInboxMessages)] int Limit = 50,
    [MaxLength(320)] string? Recipient = null,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)] string? Category = null,
    bool? Unread = null);

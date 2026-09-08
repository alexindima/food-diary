using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminOutgoingEmailsHttpQuery(
    [OpenApiNumericRange(1, 10000)] int Page = 1,
    [OpenApiNumericRange(1, 100)] int Limit = 50,
    [MaxLength(64)] string? Purpose = null,
    [MaxLength(32)] string? Status = null,
    [MaxLength(320)] string? Recipient = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, Guid? Id = null, [MaxLength(256)] string? CorrelationId = null);

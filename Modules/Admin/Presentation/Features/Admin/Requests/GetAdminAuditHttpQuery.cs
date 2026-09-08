using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetAdminAuditHttpQuery([OpenApiNumericRange(1, 10000)] int Page = 1, [OpenApiNumericRange(1, 100)] int Limit = 50,
    DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, Guid? ActorUserId = null, Guid? SubjectClientUserId = null,
    [MaxLength(200)] string? Action = null, [MaxLength(100)] string? TargetType = null, [MaxLength(200)] string? TargetId = null);

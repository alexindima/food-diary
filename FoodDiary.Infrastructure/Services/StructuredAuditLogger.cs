using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Services;

internal sealed class StructuredAuditLogger(
    ILogger<StructuredAuditLogger> logger,
    IDateTimeProvider dateTimeProvider) : IAuditLogger {
    public void Log(string action, UserId actorId, string? targetType, string? targetId, string? details) {
        logger.LogInformation(
            "AUDIT action={Action} actor={ActorId} targetType={TargetType} targetId={TargetId} details={Details} timestamp={Timestamp}",
            action,
            actorId.Value,
            targetType ?? "-",
            targetId ?? "-",
            details ?? "-",
            dateTimeProvider.UtcNow.ToString("O"));
    }
}

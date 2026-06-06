using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Services;

internal sealed class StructuredAuditLogger(
    ILogger<StructuredAuditLogger> logger,
    TimeProvider dateTimeProvider) : IAuditLogger {
    public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) {
        logger.LogInformation(
            "AUDIT action={Action} actor={ActorId} targetType={TargetType} targetId={TargetId} details={Details} timestamp={Timestamp}",
            action,
            actorId.Value,
            targetType ?? "-",
            targetId ?? "-",
            details ?? "-",
            dateTimeProvider.GetUtcNow().UtcDateTime.ToString("O"));
    }
}

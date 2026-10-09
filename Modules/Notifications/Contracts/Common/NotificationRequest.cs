using System.Diagnostics.CodeAnalysis;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Contracts.Common;

[ExcludeFromCodeCoverage]
public sealed record NotificationRequest {
    public UserId UserId { get; }
    public NotificationIntent Intent { get; }
    public string Type => Intent.Type;
    public string PayloadJson => Intent.PayloadJson;
    public string? ReferenceId => Intent.ReferenceId;

    public NotificationRequest(UserId userId, NotificationIntent intent) {
        ArgumentNullException.ThrowIfNull(intent);
        UserId = userId;
        Intent = intent;
    }

    public NotificationRequest(UserId userId, string type, string payloadJson, string? referenceId = null)
        : this(userId, NotificationIntent.FromLegacy(type, payloadJson, referenceId)) {
    }
}

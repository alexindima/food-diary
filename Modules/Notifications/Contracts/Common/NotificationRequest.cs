using System.Diagnostics.CodeAnalysis;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Contracts.Common;

[ExcludeFromCodeCoverage]
public sealed record NotificationRequest(
    UserId UserId,
    string Type,
    string PayloadJson,
    string? ReferenceId = null);

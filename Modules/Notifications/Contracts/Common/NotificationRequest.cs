using System.Diagnostics.CodeAnalysis;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

[ExcludeFromCodeCoverage]
public sealed record NotificationRequest(
    UserId UserId,
    string Type,
    string PayloadJson,
    string? ReferenceId = null);

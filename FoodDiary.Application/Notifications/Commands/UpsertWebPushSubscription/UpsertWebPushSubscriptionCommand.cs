using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;

public sealed record UpsertWebPushSubscriptionCommand(
    Guid? UserId,
    string Endpoint,
    string P256Dh,
    string Auth,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent) : ICommand<Result>, IUserRequest;

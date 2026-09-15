using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Notifications.Application.Commands.UpsertWebPushSubscription;

public sealed record UpsertWebPushSubscriptionCommand(
    Guid? UserId,
    string Endpoint,
    string P256Dh,
    string Auth,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent) : ICommand<Result>, IUserRequest;

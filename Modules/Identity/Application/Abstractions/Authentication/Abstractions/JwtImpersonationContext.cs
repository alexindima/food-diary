using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public sealed record JwtImpersonationContext(UserId ActorUserId, string Reason);

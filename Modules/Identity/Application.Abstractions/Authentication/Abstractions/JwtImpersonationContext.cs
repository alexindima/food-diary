using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record JwtImpersonationContext(UserId ActorUserId, string Reason);

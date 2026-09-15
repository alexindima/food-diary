using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Abstractions.Models;

public sealed record UserCleanupCursor(DateTime DeletedAtUtc, UserId UserId);

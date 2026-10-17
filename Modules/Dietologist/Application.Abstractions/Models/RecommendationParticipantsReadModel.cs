using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Models;

public sealed record RecommendationParticipantsReadModel(UserId ClientUserId, UserId DietologistUserId);

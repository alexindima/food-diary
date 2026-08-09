using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminAchievementDefinitions;

public sealed record GetAdminAchievementDefinitionsQuery : IQuery<Result<IReadOnlyList<AchievementDefinitionAdminModel>>>;

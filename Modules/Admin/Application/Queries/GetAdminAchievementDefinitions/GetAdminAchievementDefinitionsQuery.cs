using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAchievementDefinitions;

public sealed record GetAdminAchievementDefinitionsQuery(int Page = 1, int Limit = 50) : IQuery<Result<IReadOnlyList<AchievementDefinitionAdminModel>>>;

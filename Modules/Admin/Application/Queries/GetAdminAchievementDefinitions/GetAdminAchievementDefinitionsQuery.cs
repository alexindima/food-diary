using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAchievementDefinitions;

public sealed record GetAdminAchievementDefinitionsQuery : IQuery<Result<IReadOnlyList<AchievementDefinitionAdminModel>>>;

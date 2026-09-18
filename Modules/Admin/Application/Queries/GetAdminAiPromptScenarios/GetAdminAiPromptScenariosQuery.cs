using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPromptScenarios;

public sealed record GetAdminAiPromptScenariosQuery : IQuery<Result<IReadOnlyList<AdminAiPromptScenarioModel>>>;

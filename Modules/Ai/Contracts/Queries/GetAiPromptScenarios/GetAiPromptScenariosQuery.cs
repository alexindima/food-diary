using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptScenarios;

public sealed record GetAiPromptScenariosQuery : IRequest<IReadOnlyList<AiPromptScenarioModel>>;

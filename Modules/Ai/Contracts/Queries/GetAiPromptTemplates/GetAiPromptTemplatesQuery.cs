using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptTemplates;

public sealed record GetAiPromptTemplatesQuery : IRequest<IReadOnlyList<AiPromptTemplateReadModel>>;

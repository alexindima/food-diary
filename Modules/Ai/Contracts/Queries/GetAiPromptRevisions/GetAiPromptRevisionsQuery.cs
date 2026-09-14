using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Contracts.Queries.GetAiPromptRevisions;

public sealed record GetAiPromptRevisionsQuery(
    string Key,
    string Locale) : IRequest<IReadOnlyList<AiPromptRevisionReadModel>>;

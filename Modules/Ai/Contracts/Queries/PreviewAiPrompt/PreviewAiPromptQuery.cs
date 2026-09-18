using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Contracts.Queries.PreviewAiPrompt;

public sealed record PreviewAiPromptQuery(AiPromptDraft Draft) : IRequest<Result<string>>;

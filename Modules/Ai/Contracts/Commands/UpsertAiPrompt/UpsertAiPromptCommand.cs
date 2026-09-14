using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record UpsertAiPromptCommand(
    string Key,
    string Locale,
    string PromptText,
    bool IsActive) : IRequest<Result<AiPromptTemplateReadModel>>;

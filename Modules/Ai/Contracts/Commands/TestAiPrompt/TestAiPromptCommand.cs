using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Contracts.Commands.TestAiPrompt;

public sealed record TestAiPromptCommand(Guid UserId, string RequestId, AiPromptDraft Draft) : IRequest<Result<string>>;

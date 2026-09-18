using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
namespace FoodDiary.Modules.Admin.Application.Commands.TestAdminAiPrompt;

public sealed record TestAdminAiPromptCommand(Guid UserId, string RequestId, AdminAiPromptDraft Draft) : IRequest<Result<string>>;

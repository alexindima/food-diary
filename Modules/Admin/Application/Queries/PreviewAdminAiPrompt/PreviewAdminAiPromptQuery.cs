using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
namespace FoodDiary.Modules.Admin.Application.Queries.PreviewAdminAiPrompt;

public sealed record PreviewAdminAiPromptQuery(AdminAiPromptDraft Draft) : IRequest<Result<string>>;

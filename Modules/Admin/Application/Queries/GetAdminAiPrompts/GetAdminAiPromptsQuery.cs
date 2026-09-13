using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPrompts;

public record GetAdminAiPromptsQuery : IQuery<Result<IReadOnlyList<AdminAiPromptModel>>>;

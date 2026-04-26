using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;

public record GetAdminAiPromptsQuery() : IQuery<Result<IReadOnlyList<AdminAiPromptModel>>>;

using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPrompts;

public record GetAdminAiPromptsQuery(int Page = 1, int Limit = 50) : IQuery<Result<IReadOnlyList<AdminAiPromptModel>>>;

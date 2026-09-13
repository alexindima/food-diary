using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.UpsertAdminAiPrompt;

public record UpsertAdminAiPromptCommand(
    string Key,
    string Locale,
    string PromptText,
    bool IsActive) : ICommand<Result<AdminAiPromptModel>>;

using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;

public record UpsertAdminAiPromptCommand(
    string Key,
    string Locale,
    string PromptText,
    bool IsActive) : ICommand<Result<AdminAiPromptModel>>;

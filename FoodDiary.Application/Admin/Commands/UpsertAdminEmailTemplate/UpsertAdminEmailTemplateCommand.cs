using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;

public sealed record UpsertAdminEmailTemplateCommand(
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive)
    : ICommand<Result<AdminEmailTemplateModel>>;

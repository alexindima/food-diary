using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Commands.UpsertAdminEmailTemplate;

public sealed record UpsertAdminEmailTemplateCommand(
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive)
    : ICommand<Result<AdminEmailTemplateModel>>;

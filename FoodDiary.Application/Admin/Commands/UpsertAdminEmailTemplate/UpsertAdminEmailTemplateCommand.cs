using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;

public sealed record UpsertAdminEmailTemplateCommand(
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive)
    : ICommand<Result<AdminEmailTemplateResponse>>;

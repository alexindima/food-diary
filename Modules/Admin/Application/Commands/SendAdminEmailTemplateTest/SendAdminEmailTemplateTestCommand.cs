using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.SendAdminEmailTemplateTest;

public sealed record SendAdminEmailTemplateTestCommand(
    string ToEmail,
    string Key,
    string Subject,
    string HtmlBody,
    string TextBody)
    : ICommand<Result>;

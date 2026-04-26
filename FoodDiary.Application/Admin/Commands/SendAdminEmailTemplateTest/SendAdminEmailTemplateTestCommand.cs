using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;

public sealed record SendAdminEmailTemplateTestCommand(
    string ToEmail,
    string Key,
    string Subject,
    string HtmlBody,
    string TextBody)
    : ICommand<Result>;

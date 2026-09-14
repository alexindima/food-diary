using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Email.Commands.UpsertEmailTemplate;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record UpsertEmailTemplateCommand(
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive) : IRequest<Result<EmailTemplateReadModel>>;

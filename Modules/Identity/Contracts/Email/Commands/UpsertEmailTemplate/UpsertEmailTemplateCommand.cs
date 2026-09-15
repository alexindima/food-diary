using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Contracts.Email.Commands.UpsertEmailTemplate;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record UpsertEmailTemplateCommand(
    string Key,
    string Locale,
    string Subject,
    string HtmlBody,
    string TextBody,
    bool IsActive) : IRequest<Result<EmailTemplateReadModel>>;

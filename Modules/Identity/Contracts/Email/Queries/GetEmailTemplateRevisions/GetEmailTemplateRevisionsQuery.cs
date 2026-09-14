using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplateRevisions;

public sealed record GetEmailTemplateRevisionsQuery(
    string Key,
    string Locale) : IRequest<IReadOnlyList<EmailTemplateRevisionReadModel>>;

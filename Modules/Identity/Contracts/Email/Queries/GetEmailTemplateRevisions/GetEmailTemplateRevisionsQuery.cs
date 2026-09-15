using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplateRevisions;

public sealed record GetEmailTemplateRevisionsQuery(
    string Key,
    string Locale) : IRequest<IReadOnlyList<EmailTemplateRevisionReadModel>>;

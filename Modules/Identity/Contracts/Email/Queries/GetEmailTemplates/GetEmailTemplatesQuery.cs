using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates;

public sealed record GetEmailTemplatesQuery : IRequest<IReadOnlyList<EmailTemplateReadModel>>;

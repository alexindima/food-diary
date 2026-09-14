using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplates;

public sealed record GetEmailTemplatesQuery : IRequest<IReadOnlyList<EmailTemplateReadModel>>;

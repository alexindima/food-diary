using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates;

namespace FoodDiary.Modules.Identity.Application.Email.Queries.GetEmailTemplates;

public sealed class GetEmailTemplatePageQueryHandler(IEmailTemplateReadModelRepository repository)
    : IRequestHandler<GetEmailTemplatePageQuery, IReadOnlyList<EmailTemplateReadModel>> {
    public Task<IReadOnlyList<EmailTemplateReadModel>> Handle(
        GetEmailTemplatePageQuery request,
        CancellationToken cancellationToken) =>
        repository.GetPageReadModelsAsync(
            PaginationPolicy.NormalizePage(request.Page),
            PaginationPolicy.NormalizePageSize(request.Limit),
            cancellationToken);
}

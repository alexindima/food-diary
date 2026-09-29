using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Contracts.Email.Queries.GetEmailTemplates;
using FoodDiary.Modules.Identity.Contracts.Admin.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminEmailTemplates;

public sealed class GetAdminEmailTemplatesQueryHandler(ISender emailTemplateReadService)
    : IQueryHandler<GetAdminEmailTemplatesQuery, Result<IReadOnlyList<AdminEmailTemplateModel>>> {
    public async Task<Result<IReadOnlyList<AdminEmailTemplateModel>>> Handle(GetAdminEmailTemplatesQuery query, CancellationToken cancellationToken) {
        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        IReadOnlyList<EmailTemplateReadModel> templates = await emailTemplateReadService.Send(new GetEmailTemplatePageQuery(page, limit), cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminEmailTemplateModel>>(templates.Select(static template => template.ToAdminModel()).ToList());
    }
}

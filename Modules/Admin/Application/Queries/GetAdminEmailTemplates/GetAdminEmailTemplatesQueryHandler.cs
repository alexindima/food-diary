using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Email.Queries.GetEmailTemplates;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminEmailTemplates;

public sealed class GetAdminEmailTemplatesQueryHandler(ISender emailTemplateReadService)
    : IQueryHandler<GetAdminEmailTemplatesQuery, Result<IReadOnlyList<AdminEmailTemplateModel>>> {
    public async Task<Result<IReadOnlyList<AdminEmailTemplateModel>>> Handle(GetAdminEmailTemplatesQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<EmailTemplateReadModel> templates = await emailTemplateReadService.Send(new GetEmailTemplatesQuery(), cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminEmailTemplateModel>>(templates.Select(static template => template.ToAdminModel()).ToList());
    }
}

using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;

public sealed class GetAdminEmailTemplatesQueryHandler(IEmailTemplateAdministrationReadService emailTemplateReadService)
    : IQueryHandler<GetAdminEmailTemplatesQuery, Result<IReadOnlyList<AdminEmailTemplateModel>>> {
    public async Task<Result<IReadOnlyList<AdminEmailTemplateModel>>> Handle(GetAdminEmailTemplatesQuery query, CancellationToken cancellationToken) {
        IReadOnlyList<EmailTemplateReadModel> templates = await emailTemplateReadService
            .GetTemplatesAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminEmailTemplateModel>>(templates.Select(static template => template.ToAdminModel()).ToList());
    }
}

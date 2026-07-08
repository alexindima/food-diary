using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;

public sealed class GetAdminEmailTemplatesQueryHandler(
    IAdminContentReadService adminContentReadService)
    : IQueryHandler<GetAdminEmailTemplatesQuery, Result<IReadOnlyList<AdminEmailTemplateModel>>> {
    public async Task<Result<IReadOnlyList<AdminEmailTemplateModel>>> Handle(
        GetAdminEmailTemplatesQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<AdminEmailTemplateModel> response = await adminContentReadService
            .GetEmailTemplatesAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(response);
    }
}

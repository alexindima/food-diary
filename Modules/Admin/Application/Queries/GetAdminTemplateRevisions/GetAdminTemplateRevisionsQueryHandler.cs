using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminTemplateRevisions;

public sealed class GetAdminTemplateRevisionsQueryHandler(IAdminContentReadService service)
    : IQueryHandler<GetAdminTemplateRevisionsQuery, Result<IReadOnlyList<AdminTemplateRevisionModel>>> {
    public async Task<Result<IReadOnlyList<AdminTemplateRevisionModel>>> Handle(GetAdminTemplateRevisionsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await service.GetTemplateRevisionsAsync(query.Key.Trim().ToLowerInvariant(), query.Locale.Trim().ToLowerInvariant(),
            query.IsAiPrompt, cancellationToken).ConfigureAwait(false));
}

using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Dietologist.Application.Queries.SearchRecommendationTemplates;

public sealed class SearchRecommendationTemplatesQueryHandler(
    IRecommendationTemplateReadModelRepository repository,
    ICurrentUserAccessService userContextService)
    : IQueryHandler<SearchRecommendationTemplatesQuery, Result<IReadOnlyList<RecommendationTemplateModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationTemplateModel>>> Handle(
        SearchRecommendationTemplatesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationTemplateModel>>(userIdResult);
        }

        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        IReadOnlyList<RecommendationTemplateReadModel> templates = await repository.SearchAsync(
            userIdResult.Value,
            query.Search,
            query.IncludeArchived,
            page,
            limit,
            cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<RecommendationTemplateModel>>([.. templates.Select(template => template.ToModel())]);
    }
}

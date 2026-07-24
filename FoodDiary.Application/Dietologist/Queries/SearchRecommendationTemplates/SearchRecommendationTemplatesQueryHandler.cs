using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.SearchRecommendationTemplates;

public sealed class SearchRecommendationTemplatesQueryHandler(
    IRecommendationTemplateReadService readService,
    IUserContextService userContextService)
    : IQueryHandler<SearchRecommendationTemplatesQuery, Result<IReadOnlyList<RecommendationTemplateModel>>> {
    public async Task<Result<IReadOnlyList<RecommendationTemplateModel>>> Handle(
        SearchRecommendationTemplatesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<RecommendationTemplateModel>>(userIdResult);
        }

        IReadOnlyList<RecommendationTemplateModel> templates = await readService.SearchAsync(
            userIdResult.Value,
            query.Search,
            query.IncludeArchived,
            cancellationToken).ConfigureAwait(false);
        return Result.Success(templates);
    }
}

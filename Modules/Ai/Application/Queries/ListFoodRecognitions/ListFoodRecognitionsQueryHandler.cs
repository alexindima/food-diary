using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions;

public sealed class ListFoodRecognitionsQueryHandler(IFoodRecognitionJobReader store)
    : IQueryHandler<ListFoodRecognitionsQuery, Result<PagedResponse<FoodRecognitionJobModel>>> {
    public async Task<Result<PagedResponse<FoodRecognitionJobModel>>> Handle(ListFoodRecognitionsQuery request, CancellationToken cancellationToken) {
        int page = PaginationPolicy.NormalizePage(request.Page);
        int limit = PaginationPolicy.NormalizePageSize(request.Limit, defaultPageSize: 20);
        (IReadOnlyList<FoodRecognitionJobModel> items, int total) = await store.ListAsync(request.UserId, page, limit, request.IsProductLabel, cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedResponse<FoodRecognitionJobModel>(items, page, limit, (int)Math.Ceiling(total / (double)limit), total));
    }
}

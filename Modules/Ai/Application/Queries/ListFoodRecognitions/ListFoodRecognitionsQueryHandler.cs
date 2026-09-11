using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Ai.Queries.ListFoodRecognitions;

public sealed class ListFoodRecognitionsQueryHandler(IFoodRecognitionJobStore store)
    : IQueryHandler<ListFoodRecognitionsQuery, Result<IReadOnlyList<FoodRecognitionJobModel>>> {
    public async Task<Result<IReadOnlyList<FoodRecognitionJobModel>>> Handle(ListFoodRecognitionsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await store.ListAsync(request.UserId, cancellationToken).ConfigureAwait(false));
}

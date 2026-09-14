using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Queries.ListFoodRecognitions;

public sealed class ListFoodRecognitionsQueryHandler(IFoodRecognitionJobStore store)
    : IQueryHandler<ListFoodRecognitionsQuery, Result<IReadOnlyList<FoodRecognitionJobModel>>> {
    public async Task<Result<IReadOnlyList<FoodRecognitionJobModel>>> Handle(ListFoodRecognitionsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await store.ListAsync(request.UserId, cancellationToken).ConfigureAwait(false));
}

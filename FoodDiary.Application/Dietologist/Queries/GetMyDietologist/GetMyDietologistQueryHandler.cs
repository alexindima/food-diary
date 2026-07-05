using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologist;

public sealed class GetMyDietologistQueryHandler(IDietologistInvitationReadService readService)
    : IQueryHandler<GetMyDietologistQuery, Result<DietologistInfoModel?>> {
    public async Task<Result<DietologistInfoModel?>> Handle(GetMyDietologistQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistInfoModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        return await readService.GetMyDietologistAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}

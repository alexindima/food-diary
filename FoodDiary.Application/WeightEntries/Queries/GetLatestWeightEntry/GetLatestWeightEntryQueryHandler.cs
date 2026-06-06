using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public class GetLatestWeightEntryQueryHandler(
    IWeightEntryRepository weightEntryRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetLatestWeightEntryQuery, Result<WeightEntryModel?>> {
    public async Task<Result<WeightEntryModel?>> Handle(
        GetLatestWeightEntryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<WeightEntryModel?>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<WeightEntryModel?>(accessError);
        }

        IReadOnlyList<WeightEntry> entries = await weightEntryRepository.GetEntriesAsync(
            userId,
            dateFrom: null,
            dateTo: null,
            limit: 1,
            descending: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        WeightEntry? latest = entries.FirstOrDefault();
        return Result.Success(latest?.ToModel());
    }
}

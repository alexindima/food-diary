using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public class GetWeightEntriesQueryHandler(
    IWeightEntryRepository weightEntryRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetWeightEntriesQuery, Result<IReadOnlyList<WeightEntryModel>>> {
    public async Task<Result<IReadOnlyList<WeightEntryModel>>> Handle(
        GetWeightEntriesQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WeightEntryModel>>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<WeightEntryModel>>(accessError);
        }

        DateTime? normalizedFrom = query.DateFrom.HasValue
            ? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom.Value)
            : null;
        DateTime? normalizedTo = query.DateTo.HasValue
            ? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo.Value)
            : null;

        IReadOnlyList<WeightEntry> entries = await weightEntryRepository.GetEntriesAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            query.Limit,
            query.Descending,
            cancellationToken).ConfigureAwait(false);

        var response = entries.Select(entry => entry.ToModel()).ToList();
        return Result.Success<IReadOnlyList<WeightEntryModel>>(response);
    }
}

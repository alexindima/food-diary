using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;

public class GetWaistEntriesQueryHandler(
    IWaistEntryRepository waistEntryRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetWaistEntriesQuery, Result<IReadOnlyList<WaistEntryModel>>> {
    public async Task<Result<IReadOnlyList<WaistEntryModel>>> Handle(
        GetWaistEntriesQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<WaistEntryModel>>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<WaistEntryModel>>(accessError);
        }

        var normalizedFrom = query.DateFrom.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateFrom.Value)
            : null;
        var normalizedTo = query.DateTo.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(query.DateTo.Value)
            : null;

        var entries = await waistEntryRepository.GetEntriesAsync(
            userId,
            normalizedFrom,
            normalizedTo,
            query.Limit,
            query.Descending,
            cancellationToken).ConfigureAwait(false);

        var response = entries.Select(entry => entry.ToModel()).ToList();
        return Result.Success<IReadOnlyList<WaistEntryModel>>(response);
    }
}

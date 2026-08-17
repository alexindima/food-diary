using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using System.Globalization;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Cycles.Common;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Export.Queries.ExportCycle;

public sealed class ExportCycleQueryHandler(
    ICycleReadService cycleReadService,
    ICurrentUserAccessService currentUserAccessService,
    IUserLookupRepository? userLookupRepository = null,
    IPasswordHasher? passwordHasher = null)
    : IQueryHandler<ExportCycleQuery, Result<FileExportResult>> {
    private const int MaxExportRangeDays = 366;

    public async Task<Result<FileExportResult>> Handle(
        ExportCycleQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FileExportResult>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        if (query.DateFrom > query.DateTo) {
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateFrom), "DateFrom must be less than or equal to DateTo."));
        }

        if (query.DateTo.DayNumber - query.DateFrom.DayNumber > MaxExportRangeDays) {
            return Result.Failure<FileExportResult>(
                Errors.Validation.Invalid(nameof(query.DateTo), "Export range must not exceed one year."));
        }

        CycleModel? cycle = await cycleReadService.GetCurrentAsync(userId, cancellationToken).ConfigureAwait(false);
        if (cycle is null) {
            return Result.Failure<FileExportResult>(Errors.Cycle.NotFound(Guid.Empty));
        }

        if (query.Scope == CycleExportScope.Sensitive) {
            if (userLookupRepository is null || passwordHasher is null) {
                return Result.Failure<FileExportResult>(
                    Errors.Validation.Invalid(nameof(query.Scope), "Sensitive export is not configured."));
            }

            User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (user is null) {
                return Result.Failure<FileExportResult>(Errors.User.NotFound(userId.Value));
            }

            if (!user.HasPassword) {
                return Result.Failure<FileExportResult>(Errors.User.PasswordNotSet);
            }

            if (!passwordHasher.Verify(query.CurrentPassword ?? string.Empty, user.Password)) {
                return Result.Failure<FileExportResult>(Errors.User.InvalidPassword);
            }
        }

        string fromStr = query.DateFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string toStr = query.DateTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string scopeSuffix = query.Scope == CycleExportScope.Sensitive ? "-sensitive" : string.Empty;

        return Result.Success(new FileExportResult(
            CycleCsvGenerator.Generate(cycle, query.DateFrom, query.DateTo, query.Scope),
            "text/csv",
            $"cycle-tracking-{fromStr}-to-{toStr}{scopeSuffix}.csv"));
    }
}

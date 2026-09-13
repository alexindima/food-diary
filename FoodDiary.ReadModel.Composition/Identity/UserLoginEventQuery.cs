using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Infrastructure.Persistence;

namespace FoodDiary.ReadModel.Composition.Identity;

public sealed class UserLoginEventQuery(FoodDiaryDbContext context) : IUserLoginEventQuery {
    private const string LikeEscapeCharacter = "\\";

    public async Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken = default, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, string? provider = null, string? device = null) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);

        var query =
            from loginEvent in context.UserLoginEvents.AsNoTracking()
            join user in context.Users.AsNoTracking() on loginEvent.UserId equals user.Id
            select new { loginEvent, user };

        if (userId.HasValue) {
            var typedUserId = new UserId(userId.Value);
            query = query.AsNoTracking().Where(item => item.loginEvent.UserId == typedUserId);
        }

        if (!string.IsNullOrWhiteSpace(search)) {
            string term = $"%{EscapeLikePattern(search)}%";
            query = query.AsNoTracking().Where(item =>
                (item.user.Email != null && EF.Functions.ILike(item.user.Email, term, LikeEscapeCharacter)) ||
                EF.Functions.ILike(item.loginEvent.AuthProvider, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.IpAddress ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.BrowserName ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.OperatingSystem ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.DeviceType ?? string.Empty, term, LikeEscapeCharacter));
        }

        if (fromUtc.HasValue) { DateTime start = fromUtc.Value.UtcDateTime; query = query.AsNoTracking().Where(item => item.loginEvent.LoggedInAtUtc >= start); }
        if (toUtc.HasValue) { DateTime end = toUtc.Value.UtcDateTime; query = query.AsNoTracking().Where(item => item.loginEvent.LoggedInAtUtc < end); }
        if (!string.IsNullOrWhiteSpace(provider)) { query = query.AsNoTracking().Where(item => item.loginEvent.AuthProvider == provider); }
        if (!string.IsNullOrWhiteSpace(device)) { query = query.AsNoTracking().Where(item => item.loginEvent.DeviceType == device); }
        int total = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        List<UserLoginEventReadModel> items = await query.AsNoTracking()
            .OrderByDescending(item => item.loginEvent.LoggedInAtUtc)
            .ThenBy(item => item.loginEvent.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new UserLoginEventReadModel(
                item.loginEvent.Id,
                item.user.Id.Value,
                item.user.Email,
                item.loginEvent.AuthProvider,
                item.loginEvent.IpAddress,
                item.loginEvent.UserAgent,
                item.loginEvent.BrowserName,
                item.loginEvent.BrowserVersion,
                item.loginEvent.OperatingSystem,
                item.loginEvent.DeviceType,
                item.loginEvent.LoggedInAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default) {
        IQueryable<UserLoginEvent> query = context.UserLoginEvents.AsNoTracking().AsQueryable();

        if (fromUtc.HasValue) {
            query = query.AsNoTracking().Where(item => item.LoggedInAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue) {
            query = query.AsNoTracking().Where(item => item.LoggedInAtUtc <= toUtc.Value);
        }

        List<UserLoginDeviceSummaryModel> byDeviceType = await query.AsNoTracking()
            .GroupBy(item => item.DeviceType ?? "Unknown")
            .Select(group => new UserLoginDeviceSummaryModel(
                $"device:{group.Key}",
                group.Count(),
                group.Max(item => item.LoggedInAtUtc)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        List<UserLoginDeviceSummaryModel> byBrowser = await query.AsNoTracking()
            .GroupBy(item => item.BrowserName ?? "Unknown")
            .Select(group => new UserLoginDeviceSummaryModel(
                $"browser:{group.Key}",
                group.Count(),
                group.Max(item => item.LoggedInAtUtc)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        List<UserLoginDeviceSummaryModel> byOperatingSystem = await query.AsNoTracking()
            .GroupBy(item => item.OperatingSystem ?? "Unknown")
            .Select(group => new UserLoginDeviceSummaryModel(
                $"os:{group.Key}",
                group.Count(),
                group.Max(item => item.LoggedInAtUtc)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return byDeviceType
            .Concat(byBrowser)
            .Concat(byOperatingSystem)
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Trim()
            .Replace("\\", @"\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}

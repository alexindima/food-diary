using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserLoginEventRepository(FoodDiaryDbContext context) : IUserLoginEventRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(UserLoginEvent loginEvent, CancellationToken cancellationToken = default) {
        context.UserLoginEvents.Add(loginEvent);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Clamp(limit, 1, 100);

        var query =
            from loginEvent in context.UserLoginEvents.AsNoTracking()
            join user in context.Users.AsNoTracking() on loginEvent.UserId equals user.Id
            select new { loginEvent, user };

        if (userId.HasValue) {
            var typedUserId = new UserId(userId.Value);
            query = query.Where(item => item.loginEvent.UserId == typedUserId);
        }

        if (!string.IsNullOrWhiteSpace(search)) {
            string term = $"%{EscapeLikePattern(search)}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.user.Email, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.AuthProvider, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.IpAddress ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.BrowserName ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.OperatingSystem ?? string.Empty, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.loginEvent.DeviceType ?? string.Empty, term, LikeEscapeCharacter));
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<UserLoginEventReadModel> items = await query
            .OrderByDescending(item => item.loginEvent.LoggedInAtUtc)
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
            query = query.Where(item => item.LoggedInAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue) {
            query = query.Where(item => item.LoggedInAtUtc <= toUtc.Value);
        }

        List<UserLoginDeviceSummaryModel> byDeviceType = await query
            .GroupBy(item => item.DeviceType ?? "Unknown")
            .Select(group => new UserLoginDeviceSummaryModel(
                $"device:{group.Key}",
                group.Count(),
                group.Max(item => item.LoggedInAtUtc)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        List<UserLoginDeviceSummaryModel> byBrowser = await query
            .GroupBy(item => item.BrowserName ?? "Unknown")
            .Select(group => new UserLoginDeviceSummaryModel(
                $"browser:{group.Key}",
                group.Count(),
                group.Max(item => item.LoggedInAtUtc)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        List<UserLoginDeviceSummaryModel> byOperatingSystem = await query
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

    public async Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default) {
        Guid[] ids = await context.UserLoginEvents
            .AsNoTracking()
            .Where(item => item.LoggedInAtUtc < olderThanUtc)
            .OrderBy(item => item.LoggedInAtUtc)
            .Select(item => item.Id)
            .Take(Math.Max(batchSize, 1))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        if (ids.Length == 0) {
            return 0;
        }

        return await context.UserLoginEvents
            .Where(item => ids.Contains(item.Id))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}

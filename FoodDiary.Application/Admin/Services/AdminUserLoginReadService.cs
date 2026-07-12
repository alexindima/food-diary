using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminUserLoginReadService(IAuthenticationLoginEventReadService readService) : IAdminUserLoginReadService {
    public async Task<Result<PagedResponse<AdminUserLoginEventModel>>> GetEventsAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken) {
        int normalizedPage = page <= 0 ? 1 : page;
        int normalizedLimit = limit is > 0 and <= 100 ? limit : 20;
        (IReadOnlyList<UserLoginEventReadModel> items, int totalItems) =
            await readService.GetEventsAsync(normalizedPage, normalizedLimit, userId, search, cancellationToken).ConfigureAwait(false);
        AdminUserLoginEventModel[] models = [.. items.Select(ToModel)];
        int totalPages = (int)Math.Ceiling(totalItems / (double)normalizedLimit);
        return Result.Success(new PagedResponse<AdminUserLoginEventModel>(
            models,
            normalizedPage,
            normalizedLimit,
            totalPages,
            totalItems));
    }

    public async Task<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> GetSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken) {
        IReadOnlyList<UserLoginDeviceSummaryModel> summary =
            await readService.GetDeviceSummaryAsync(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>(summary
            .Select(static item => new AdminUserLoginDeviceSummaryModel(item.Key, item.Count, item.LastSeenAtUtc))
            .ToArray());
    }

    private static AdminUserLoginEventModel ToModel(UserLoginEventReadModel model) =>
        new(
            model.Id,
            model.UserId,
            model.UserEmail,
            model.AuthProvider,
            MaskIpAddress(model.IpAddress),
            model.UserAgent,
            model.BrowserName,
            model.BrowserVersion,
            model.OperatingSystem,
            model.DeviceType,
            model.LoggedInAtUtc);

    private static string? MaskIpAddress(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string trimmed = value.Trim();
        string[] ipv4Parts = trimmed.Split('.');
        if (ipv4Parts.Length == 4) {
            return $"{ipv4Parts[0]}.{ipv4Parts[1]}.{ipv4Parts[2]}.0";
        }

        string[] ipv6Parts = trimmed.Split(':');
        return ipv6Parts.Length > 2
            ? string.Join(':', ipv6Parts.Take(4).Concat(["0000"]))
            : trimmed;
    }
}

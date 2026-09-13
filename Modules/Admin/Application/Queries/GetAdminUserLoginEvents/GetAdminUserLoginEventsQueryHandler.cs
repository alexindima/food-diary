using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserLoginEvents;

public sealed class GetAdminUserLoginEventsQueryHandler(IAuthenticationLoginEventReadService readService)
    : IQueryHandler<GetAdminUserLoginEventsQuery, Result<PagedResponse<AdminUserLoginEventModel>>> {
    public async Task<Result<PagedResponse<AdminUserLoginEventModel>>> Handle(GetAdminUserLoginEventsQuery query, CancellationToken cancellationToken) {
        int normalizedPage = PaginationPolicy.NormalizePage(query.Page);
        int normalizedLimit = PaginationPolicy.NormalizePageSizeOrDefault(query.Limit);
        (IReadOnlyList<UserLoginEventReadModel> items, int totalItems) =
            await readService.GetEventsAsync(normalizedPage, normalizedLimit, query.UserId, query.Search, cancellationToken, query.FromUtc, query.ToUtc, query.Provider, query.Device).ConfigureAwait(false);
        AdminUserLoginEventModel[] models = [.. items.Select(ToModel)];
        int totalPages = (int)Math.Ceiling(totalItems / (double)normalizedLimit);
        return Result.Success(new PagedResponse<AdminUserLoginEventModel>(
            models,
            normalizedPage,
            normalizedLimit,
            totalPages,
            totalItems));
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

using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;

public sealed class GetAdminUserLoginEventsQueryHandler(IUserLoginEventRepository repository)
    : IQueryHandler<GetAdminUserLoginEventsQuery, Result<PagedResponse<AdminUserLoginEventModel>>> {
    public async Task<Result<PagedResponse<AdminUserLoginEventModel>>> Handle(
        GetAdminUserLoginEventsQuery query,
        CancellationToken cancellationToken) {
        var page = query.Page <= 0 ? 1 : query.Page;
        var limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;
        var pageData = await repository.GetPagedAsync(page, limit, query.UserId, query.Search, cancellationToken);
        var items = pageData.Items.Select(ToModel).ToArray();
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)limit);
        return Result.Success(new PagedResponse<AdminUserLoginEventModel>(items, page, limit, totalPages, pageData.TotalItems));
    }

    private static AdminUserLoginEventModel ToModel(UserLoginEventReadModel model) {
        return new AdminUserLoginEventModel(
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
    }

    private static string? MaskIpAddress(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        var ipv4Parts = trimmed.Split('.');
        if (ipv4Parts.Length == 4) {
            return $"{ipv4Parts[0]}.{ipv4Parts[1]}.{ipv4Parts[2]}.0";
        }

        var ipv6Parts = trimmed.Split(':');
        return ipv6Parts.Length > 2
            ? string.Join(':', ipv6Parts.Take(4).Concat(["0000"]))
            : trimmed;
    }
}

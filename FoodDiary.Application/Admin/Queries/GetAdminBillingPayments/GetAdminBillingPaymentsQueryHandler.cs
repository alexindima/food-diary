using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;

public sealed class GetAdminBillingPaymentsQueryHandler(IAdminBillingRepository billingRepository)
    : IQueryHandler<GetAdminBillingPaymentsQuery, Result<PagedResponse<AdminBillingPaymentReadModel>>> {
    public async Task<Result<PagedResponse<AdminBillingPaymentReadModel>>> Handle(
        GetAdminBillingPaymentsQuery query,
        CancellationToken cancellationToken) {
        var filter = new AdminBillingListFilter(
            query.Page <= 0 ? 1 : query.Page,
            query.Limit is > 0 and <= 100 ? query.Limit : 20,
            Normalize(query.Provider),
            Normalize(query.Status),
            Normalize(query.Kind),
            Normalize(query.Search),
            NormalizeUtc(query.FromUtc),
            NormalizeUtc(query.ToUtc));
        var pageData = await billingRepository.GetPaymentsAsync(filter, cancellationToken);
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)filter.Limit);
        return Result.Success(new PagedResponse<AdminBillingPaymentReadModel>(
            pageData.Items,
            filter.Page,
            filter.Limit,
            totalPages,
            pageData.TotalItems));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value?.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value?.ToUniversalTime();
}

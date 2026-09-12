using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminDashboardMetricsReader(FoodDiaryDbContext context) : IAdminDashboardMetricsReader {
    public async Task<AdminDashboardMetrics> GetAsync(DateTime fromUtc, DateTime toUtc, bool monthly, CancellationToken cancellationToken) {
        IQueryable<User> users = context.Users.AsNoTracking().Where(user => user.CreatedOnUtc >= fromUtc && user.CreatedOnUtc < toUtc);
        IQueryable<BillingPayment> payments = context.BillingPayments.AsNoTracking().Where(payment =>
            (payment.OccurredAtUtc ?? payment.CreatedOnUtc) >= fromUtc &&
            (payment.OccurredAtUtc ?? payment.CreatedOnUtc) < toUtc &&
            payment.Kind == BillingPaymentKinds.Transaction && payment.Status == "completed" && payment.Amount != null && payment.Currency != null);
        IQueryable<AiUsage> usage = context.AiUsages.AsNoTracking().Where(item => item.CreatedOnUtc >= fromUtc && item.CreatedOnUtc < toUtc);
        int payingUsers = await payments.AsNoTracking().Where(payment => payment.Amount > 0).Select(payment => payment.UserId).Distinct().CountAsync(cancellationToken).ConfigureAwait(false);
        var registrations = await users.AsNoTracking().GroupBy(user => user.CreatedOnUtc.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var tokens = await usage.AsNoTracking().GroupBy(item => item.CreatedOnUtc.Date)
            .Select(group => new { Date = group.Key, Total = group.Sum(item => (long)item.TotalTokens) }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var revenue = await payments.AsNoTracking().GroupBy(payment => new { (payment.OccurredAtUtc ?? payment.CreatedOnUtc).Date, payment.Currency })
            .Select(group => new { group.Key.Date, group.Key.Currency, Gross = group.Sum(payment => payment.Amount ?? 0m) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        DateTime Bucket(DateTime date) => monthly ? new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc) : date;
        var registrationBuckets = registrations.GroupBy(row => Bucket(row.Date)).ToDictionary(group => group.Key, group => group.Sum(row => row.Count));
        var tokenBuckets = tokens.GroupBy(row => Bucket(row.Date)).ToDictionary(group => group.Key, group => group.Sum(row => row.Total));
        var revenueBuckets = revenue.GroupBy(row => Bucket(row.Date)).ToDictionary(group => group.Key,
            group => group.GroupBy(row => row.Currency, StringComparer.Ordinal).Select(currency => new AdminDashboardRevenuePoint(currency.Key!, currency.Sum(row => row.Gross))).ToArray());
        var trend = new List<AdminDashboardTrend>();
        // Do not generate decades of empty buckets for the all-time view.
        DateTime first = registrations.Select(row => row.Date).Concat(tokens.Select(row => row.Date)).Concat(revenue.Select(row => row.Date))
            .DefaultIfEmpty(fromUtc).Min();
        DateTime start = fromUtc == DateTime.UnixEpoch ? first : fromUtc;
        if (fromUtc != DateTime.UnixEpoch || registrations.Count + tokens.Count + revenue.Count > 0) {
            for (DateTime date = Bucket(start); date < toUtc; date = monthly ? date.AddMonths(1) : date.AddDays(1)) {
                trend.Add(new AdminDashboardTrend(date, registrationBuckets.GetValueOrDefault(date), tokenBuckets.GetValueOrDefault(date),
                    revenueBuckets.GetValueOrDefault(date) ?? []));
            }
        }
        return new AdminDashboardMetrics(registrations.Sum(row => row.Count), payingUsers, tokens.Sum(row => row.Total), trend);
    }
}

using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Admin;

public sealed partial class AdminBillingRepository {
    public async Task<AdminBillingCancellationReportReadModel> GetCancellationsAsync(
        AdminBillingListFilter filter, CancellationToken cancellationToken = default) {
        var query = from cancellation in context.BillingCancellations.AsNoTracking()
                    join user in context.Users.AsNoTracking() on cancellation.UserId equals user.Id
                    select new { cancellation, user };
        if (!string.IsNullOrWhiteSpace(filter.Provider)) {
            query = query.Where(item => item.cancellation.Provider == filter.Provider);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status)) {
            query = query.Where(item => item.cancellation.Outcome == filter.Status);
        }
        if (filter.FromUtc.HasValue) {
            query = query.Where(item => item.cancellation.RequestedAtUtc >= filter.FromUtc.Value);
        }
        if (filter.ToUtc.HasValue) {
            query = query.Where(item => item.cancellation.RequestedAtUtc < filter.ToUtc.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            string pattern = BuildSearchPattern(filter.Search);
            bool hasId = Guid.TryParse(filter.Search.Trim(), out Guid id);
            var userId = new UserId(id);
            query = query.Where(item =>
                (hasId && (item.cancellation.Id == id || item.user.Id == userId)) ||
                (item.user.Email != null && EF.Functions.ILike(item.user.Email, pattern, LikeEscapeCharacter)) ||
                EF.Functions.ILike(item.cancellation.ExternalSubscriptionId, pattern, LikeEscapeCharacter));
        }
        int total = await query.CountAsync(cancellationToken);
        int feedbackCount = await query.CountAsync(item => item.cancellation.FeedbackSubmittedAtUtc != null, cancellationToken);
        List<AdminBillingCancellationReasonReadModel> reasons = await query
            .GroupBy(item => item.cancellation.ReasonCode)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key)
            .Select(group => new AdminBillingCancellationReasonReadModel(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        List<AdminBillingCancellationReadModel> items = await query
            .OrderByDescending(item => item.cancellation.RequestedAtUtc).ThenByDescending(item => item.cancellation.Id)
            .Skip(GetSkipCount(filter)).Take(filter.Limit)
            .Select(item => new AdminBillingCancellationReadModel(item.cancellation.Id, item.user.Id.Value, item.user.Email,
                item.cancellation.Provider, item.cancellation.ExternalSubscriptionId, item.cancellation.Plan,
                item.cancellation.Source, item.cancellation.Outcome, item.cancellation.RequestedAtUtc, item.cancellation.EffectiveAtUtc,
                item.cancellation.ReasonCode, item.cancellation.Comment, item.cancellation.FeedbackSubmittedAtUtc))
            .ToListAsync(cancellationToken);
        return new AdminBillingCancellationReportReadModel(items, filter.Page, filter.Limit, total,
            (int)Math.Ceiling((double)total / filter.Limit), feedbackCount, reasons);
    }
}

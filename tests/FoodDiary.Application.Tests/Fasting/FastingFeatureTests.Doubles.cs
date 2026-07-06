using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [ExcludeFromCodeCoverage]
    private sealed class InMemoryFastingPlanRepository(FastingPlan? active = null) : IFastingPlanRepository {
        public List<FastingPlan> StoredPlans { get; } = active is null ? [] : [active];
        public Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken ct = default) => Task.FromResult(active);
        public Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingPlan>> GetByUserAsync(UserId userId, FastingPlanType? type = null, FastingPlanStatus? status = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(FastingPlan plan, CancellationToken ct = default) {
            StoredPlans.Add(plan);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(FastingPlan plan, CancellationToken ct = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryFastingOccurrenceRepository(FastingOccurrence? current = null) : IFastingOccurrenceRepository {
        public List<FastingOccurrence> StoredOccurrences { get; } = current is null ? [] : [current];

        public Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken ct = default) => Task.FromResult(StoredOccurrences.LastOrDefault(x => x.Status == FastingOccurrenceStatus.Active));
        public Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingOccurrence>> GetActiveAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<FastingOccurrence>>(StoredOccurrences.Where(x => x.Status == FastingOccurrenceStatus.Active).ToList());
        public Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(FastingPlanId planId, bool includeCompleted = true, CancellationToken ct = default) {
            IReadOnlyList<FastingOccurrence> occurrences = StoredOccurrences
                .Where(x => x.PlanId == planId)
                .ToList();
            return Task.FromResult(occurrences);
        }
        public Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(UserId userId, DateTime? from = null, DateTime? to = null, FastingOccurrenceStatus? status = null, CancellationToken ct = default) {
            IEnumerable<FastingOccurrence> query = StoredOccurrences.Where(x => x.UserId == userId);

            if (from.HasValue) {
                query = query.Where(x => x.StartedAtUtc >= from.Value);
            }

            if (to.HasValue) {
                query = query.Where(x => x.StartedAtUtc <= to.Value);
            }

            if (status.HasValue) {
                query = query.Where(x => x.Status == status.Value);
            }

            IReadOnlyList<FastingOccurrence> occurrences = query
                .OrderByDescending(x => x.StartedAtUtc)
                .ToList();

            return Task.FromResult(occurrences);
        }
        public Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
            UserId userId,
            int page,
            int limit,
            DateTime? from = null,
            DateTime? to = null,
            FastingOccurrenceStatus? status = null,
            CancellationToken ct = default) {
            IEnumerable<FastingOccurrence> query = StoredOccurrences.Where(x => x.UserId == userId);

            if (from.HasValue) {
                query = query.Where(x => x.StartedAtUtc >= from.Value);
            }

            if (to.HasValue) {
                query = query.Where(x => x.StartedAtUtc <= to.Value);
            }

            if (status.HasValue) {
                query = query.Where(x => x.Status == status.Value);
            }

            var ordered = query
                .OrderByDescending(x => x.StartedAtUtc)
                .ToList();

            var items = ordered
                .Skip(Math.Max(0, page - 1) * limit)
                .Take(limit)
                .ToList();

            return Task.FromResult<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)>((items, ordered.Count));
        }
        public Task AddAsync(FastingOccurrence occurrence, CancellationToken ct = default) {
            StoredOccurrences.Add(occurrence);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(FastingOccurrence occurrence, CancellationToken ct = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class PassthroughCurrentFastingOccurrenceRepository(FastingOccurrence current) : IFastingOccurrenceRepository {
        public List<FastingOccurrence> StoredOccurrences { get; } = [current];

        public Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult<FastingOccurrence?>(current);

        public Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<FastingOccurrence>> GetActiveAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<FastingOccurrence>>(StoredOccurrences);

        public Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(FastingPlanId planId, bool includeCompleted = true, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(
            UserId userId,
            DateTime? from = null,
            DateTime? to = null,
            FastingOccurrenceStatus? status = null,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
            UserId userId,
            int page,
            int limit,
            DateTime? from = null,
            DateTime? to = null,
            FastingOccurrenceStatus? status = null,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task AddAsync(FastingOccurrence occurrence, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(FastingOccurrence occurrence, CancellationToken ct = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryFastingCheckInRepository(params FastingCheckIn[] seed) : IFastingCheckInRepository {
        private readonly List<FastingCheckIn> _stored = [.. seed];
        public IReadOnlyList<FastingCheckIn> Stored => _stored;

        public Task AddAsync(FastingCheckIn checkIn, CancellationToken cancellationToken = default) {
            _stored.Add(checkIn);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
            IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<FastingCheckIn> items = _stored
                .Where(x => occurrenceIds.Contains(x.OccurrenceId))
                .OrderByDescending(x => x.CheckedInAtUtc)
                .ToList();

            return Task.FromResult(items);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingFastingAnalyticsService : IFastingAnalyticsService {
        public DateTime FromUtc { get; private set; }
        public DateTime ToUtc { get; private set; }

        public (DateTime FromUtc, DateTime ToUtc) GetDefaultHistoryWindow(DateTime nowUtc) =>
            (nowUtc.AddDays(-1), nowUtc);

        public Task<FastingStatsModel> GetStatsAsync(UserId userId, DateTime nowUtc, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FastingInsightsModel> GetInsightsAsync(
            UserId userId,
            DateTime nowUtc,
            FastingOccurrenceReadModel? current,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedResponse<FastingSessionModel>> GetHistoryAsync(
            UserId userId,
            int page,
            int limit,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken) {
            FromUtc = fromUtc;
            ToUtc = toUtc;
            return Task.FromResult(new PagedResponse<FastingSessionModel>([], page, limit, 0, 0));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemorySchedulerNotificationRepository : INotificationRepository {
        public List<Notification> Stored { get; } = [];

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(Stored.Where(x => x.UserId == userId).Take(limit).ToList());

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Notification?>(Stored.FirstOrDefault(x => x.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default) {
            Stored.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Any(x => x.UserId == userId && string.Equals(x.Type, type, StringComparison.Ordinal) && string.Equals(x.ReferenceId, referenceId, StringComparison.Ordinal)));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Count(x => x.UserId == userId && !x.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Count(x => x.UserId == userId && !x.IsRead && string.Equals(x.Type, type, StringComparison.Ordinal)));

        public Task MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemorySchedulerNotificationWriter(
        InMemorySchedulerNotificationRepository notificationRepository,
        RecordingWebPushNotificationSender webPushNotificationSender) : INotificationWriter {
        public async Task AddAsync(
            Notification notification,
            bool sendWebPush = false,
            CancellationToken cancellationToken = default) {
            await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

            if (sendWebPush) {
                await webPushNotificationSender.SendAsync(notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationPusher : INotificationPusher {
        public List<Guid> UnreadCountUsers { get; } = [];
        public List<Guid> ChangedUsers { get; } = [];

        public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) {
            UnreadCountUsers.Add(userId);
            return Task.CompletedTask;
        }

        public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) {
            ChangedUsers.Add(userId);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingWebPushNotificationSender : IWebPushNotificationSender {
        public List<Notification> Sent { get; } = [];

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
            Sent.Add(notification);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ImmediatePostCommitActionQueue : IPostCommitActionQueue {
        public bool HasActions => false;

        public void Enqueue(Func<CancellationToken, Task> action) {
            action(CancellationToken.None).GetAwaiter().GetResult();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingPostCommitActionQueue(bool hasActions) : IPostCommitActionQueue {
        public bool HasActions => hasActions;
        public int FlushCallCount { get; private set; }

        public void Enqueue(Func<CancellationToken, Task> action) {
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) {
            FlushCallCount++;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubCurrentUserAccessService(User? user) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Error? error = user switch {
                null => Errors.Authentication.InvalidToken,
                { Id: var id } when id != userId => Errors.Authentication.InvalidToken,
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                _ => null,
            };

            return Task.FromResult(error);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime? utcNow = null) : TimeProvider {
        private readonly DateTimeOffset _utcNow = new(utcNow ?? FixedNow);

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}

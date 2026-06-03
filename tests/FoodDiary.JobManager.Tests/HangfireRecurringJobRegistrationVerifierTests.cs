using FoodDiary.JobManager.Services;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class HangfireRecurringJobRegistrationVerifierTests {
    [Fact]
    public void EnsureRegistered_WhenAllExpectedJobsExist_Completes() {
        var storage = new FakeJobStorage([RecurringJobIds.ImageAssetsCleanup, RecurringJobIds.UsersCleanup]);
        var verifier = new HangfireRecurringJobRegistrationVerifier(
            storage,
            NullLogger<HangfireRecurringJobRegistrationVerifier>.Instance);

        verifier.EnsureRegistered([RecurringJobIds.ImageAssetsCleanup, RecurringJobIds.UsersCleanup]);
    }

    [Fact]
    public void EnsureRegistered_WhenExpectedJobsAreMissing_ThrowsWithMissingIds() {
        var storage = new FakeJobStorage([RecurringJobIds.ImageAssetsCleanup]);
        var verifier = new HangfireRecurringJobRegistrationVerifier(
            storage,
            NullLogger<HangfireRecurringJobRegistrationVerifier>.Instance);

        var exception = Assert.Throws<InvalidOperationException>(
            () => verifier.EnsureRegistered([RecurringJobIds.ImageAssetsCleanup, RecurringJobIds.UsersCleanup]));

        Assert.Contains(RecurringJobIds.UsersCleanup, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(RecurringJobIds.ImageAssetsCleanup, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureRegistered_WithNullExpectedJobIds_Throws() {
        var verifier = new HangfireRecurringJobRegistrationVerifier(
            new FakeJobStorage([]),
            NullLogger<HangfireRecurringJobRegistrationVerifier>.Instance);

        Assert.Throws<ArgumentNullException>(() => verifier.EnsureRegistered(null!));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeJobStorage(IReadOnlyCollection<string> registeredJobIds) : JobStorage {
        private readonly FakeStorageConnection connection = new(registeredJobIds);

        public override IStorageConnection GetConnection() => connection;

        public override IMonitoringApi GetMonitoringApi() => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeStorageConnection(IReadOnlyCollection<string> recurringJobIds) : IStorageConnection {
        public void Dispose() {
        }

        public IWriteOnlyTransaction CreateWriteTransaction() => throw new NotSupportedException();

        public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout) => throw new NotSupportedException();

        public string CreateExpiredJob(
            Job job,
            IDictionary<string, string> parameters,
            DateTime createdAt,
            TimeSpan expireIn) => throw new NotSupportedException();

        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void SetJobParameter(string id, string name, string value) => throw new NotSupportedException();

        public string GetJobParameter(string id, string name) => throw new NotSupportedException();

        public JobData GetJobData(string jobId) => throw new NotSupportedException();

        public StateData GetStateData(string jobId) => throw new NotSupportedException();

        public void AnnounceServer(string serverId, ServerContext context) => throw new NotSupportedException();

        public void RemoveServer(string serverId) => throw new NotSupportedException();

        public void Heartbeat(string serverId) => throw new NotSupportedException();

        public int RemoveTimedOutServers(TimeSpan timeOut) => throw new NotSupportedException();

        public HashSet<string> GetAllItemsFromSet(string key) =>
            string.Equals(key, "recurring-jobs", StringComparison.Ordinal)
                ? recurringJobIds.ToHashSet(StringComparer.Ordinal)
                : [];

        public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore) => throw new NotSupportedException();

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs) =>
            throw new NotSupportedException();

        public Dictionary<string, string> GetAllEntriesFromHash(string key) {
            const string prefix = "recurring-job:";
            if (!key.StartsWith(prefix, StringComparison.Ordinal)) {
                return [];
            }

            var recurringJobId = key[prefix.Length..];
            return recurringJobIds.Contains(recurringJobId, StringComparer.Ordinal)
                ? new Dictionary<string, string>(StringComparer.Ordinal) {
                    ["Cron"] = "* * * * *",
                    ["Job"] = string.Empty,
                    ["Queue"] = "default",
                    ["TimeZoneId"] = TimeZoneInfo.Utc.Id,
                    ["CreatedAt"] = DateTime.UtcNow.ToString("O"),
                }
                : [];
        }
    }
}

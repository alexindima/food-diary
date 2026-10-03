using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Persistence.Abstractions;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class EfAtomicCommandExecutor(IModuleTransactionCoordinator coordinator, SharedPersistenceDbContext context,
    TimeProvider timeProvider) : IAtomicCommandExecutor {
    private static readonly JsonSerializerOptions ReceiptJsonOptions = new(JsonSerializerDefaults.Web) {
        Converters = { new AtomicResultJsonConverter() },
    };

    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        coordinator.ExecuteAsync((_, token) => operation(token), cancellationToken);

    public Task<T> ExecuteAsync<T>(AtomicOperation identity, Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.UserId == Guid.Empty || identity.Key.Length != 64 || identity.RequestHash.Length != 64 || identity.Retention <= TimeSpan.Zero
            || identity.Retention > TimeSpan.FromDays(7)) {
            throw new ArgumentException("An atomic operation requires key/request digests and bounded retention.", nameof(identity));
        }
        return coordinator.ExecuteAsync(async (_, token) => {
            long lockKey = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(identity.Key)));
            await context.Database.ExecuteSqlInterpolatedAsync($"select pg_advisory_xact_lock({lockKey})", token).ConfigureAwait(false);
            DateTime now = timeProvider.GetUtcNow().UtcDateTime;
            AtomicCommandReceipt? receipt = await context.Set<AtomicCommandReceipt>().AsNoTracking()
                .SingleOrDefaultAsync(receipt => receipt.Key == identity.Key, token).ConfigureAwait(false);
            if (receipt is not null && receipt.ExpiresOnUtc > now) {
                if (receipt.UserId != identity.UserId || !string.Equals(receipt.RequestHash, identity.RequestHash, StringComparison.Ordinal)
                    || !string.Equals(receipt.ResponseType, typeof(T).FullName, StringComparison.Ordinal)) {
                    string failure = JsonSerializer.Serialize(Result.Failure(new Error("Idempotency.Conflict",
                        "The idempotency key was already used with a different request.", ErrorKind.Conflict)), ReceiptJsonOptions);
                    return JsonSerializer.Deserialize<T>(failure, ReceiptJsonOptions)!;
                }
                return JsonSerializer.Deserialize<T>(receipt.ResponseJson, ReceiptJsonOptions)!;
            }
            if (receipt is not null) {
                await context.Set<AtomicCommandReceipt>().Where(receipt => receipt.Key == identity.Key)
                    .ExecuteDeleteAsync(token).ConfigureAwait(false);
            }
            T result = await operation(token).ConfigureAwait(false);
            if (result is not Result { IsFailure: true }) {
                context.Add(new AtomicCommandReceipt {
                    UserId = identity.UserId,
                    Key = identity.Key,
                    RequestHash = identity.RequestHash,
                    ResponseType = typeof(T).FullName!,
                    ResponseJson = JsonSerializer.Serialize(result, ReceiptJsonOptions),
                    ExpiresOnUtc = now.Add(identity.Retention),
                });
            }
            return result;
        }, cancellationToken);
    }
}

using System.Security.Cryptography;
using System.Text;
using FoodDiary.Application.Abstractions.Authentication.Common;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Authentication;

public sealed class TelegramLoginTicketStore(
    FoodDiaryDbContext context,
    IDataProtectionProvider protectionProvider,
    TimeProvider timeProvider) : ITelegramLoginTicketStore {
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("FoodDiary.Telegram.LoginTickets.v1");

    public async Task<string> CreateAsync(
        string purpose, string browserBinding, string payload, DateTime expiresAtUtc, CancellationToken cancellationToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(browserBinding);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (purpose.Length > 64 || browserBinding.Length > 256 || Encoding.UTF8.GetByteCount(payload) > 8192 ||
            expiresAtUtc.Kind != DateTimeKind.Utc || expiresAtUtc <= now || expiresAtUtc > now.AddMinutes(15)) {
            throw new ArgumentException("Invalid Telegram login ticket parameters.", nameof(expiresAtUtc));
        }
        string ticket = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        string fingerprint = Hash(ticket);
        string bindingHash = Hash(browserBinding);
        string protectedPayload = _protector.CreateProtector(purpose, bindingHash).Protect(payload);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"TelegramLoginTickets\" WHERE \"ExpiresAtUtc\" <= {now}", cancellationToken).ConfigureAwait(false);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"TelegramLoginTickets\" (\"Fingerprint\", \"Purpose\", \"BrowserBindingHash\", \"ProtectedPayload\", \"ExpiresAtUtc\") VALUES ({fingerprint}, {purpose}, {bindingHash}, {protectedPayload}, {expiresAtUtc})",
            cancellationToken).ConfigureAwait(false);
        return ticket;
    }

    public async Task<string?> ConsumeAsync(
        string ticket, string purpose, string browserBinding, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(ticket) || ticket.Length != 43 || string.IsNullOrWhiteSpace(purpose) ||
            purpose.Length > 64 || string.IsNullOrWhiteSpace(browserBinding) || browserBinding.Length > 256) {
            return null;
        }
        string fingerprint = Hash(ticket);
        string bindingHash = Hash(browserBinding);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IQueryable<TelegramLoginTicket> query = context.Set<TelegramLoginTicket>().AsNoTracking().Where(item =>
            item.Fingerprint == fingerprint && item.Purpose == purpose &&
            item.BrowserBindingHash == bindingHash && item.ExpiresAtUtc > now);
        TelegramLoginTicket? record = await query.AsNoTracking().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        DateTime consumedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (record is null || await query.Where(item => item.ExpiresAtUtc > consumedAtUtc)
                .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false) != 1) {
            return null;
        }
        try {
            return _protector.CreateProtector(purpose, bindingHash).Unprotect(record.ProtectedPayload);
        } catch (CryptographicException) {
            return null;
        }
    }

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

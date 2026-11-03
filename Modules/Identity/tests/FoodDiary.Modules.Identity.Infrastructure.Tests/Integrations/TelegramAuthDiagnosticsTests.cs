using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Options;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramAuthDiagnosticsTests {
    private const string BotToken = "123456:private-diagnostic-test-token";
    private const string PrivateName = "private-telegram-user-name";
    private const string PrivateField = "private-attacker-field";
    private const string UserJson = "{\"id\":913579246,\"first_name\":\"private-telegram-user-name\"}";
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 13, 40, 0, TimeSpan.Zero);

    public static IEnumerable<object[]> Rejections() {
        string valid = Sign();
        yield return Case(" ", "missing_init_data", "Validation.Required");
        yield return Case(valid, "missing_bot_token", "Authentication.TelegramNotConfigured", token: " ");
        yield return Case(valid, "invalid_auth_ttl", "Authentication.TelegramNotConfigured", ttl: 0);
        yield return Case(valid + "&user=" + Uri.EscapeDataString(UserJson), "duplicate_fields");
        yield return Case(valid + $"&{PrivateField}=secret-one&{PrivateField}=secret-two", "duplicate_fields");
        yield return Case("auth_date=1&user=" + Uri.EscapeDataString(UserJson), "missing_hash");
        yield return Case("hash=%20", "empty_hash");
        yield return Case(valid + "tampered", "hash_mismatch");
        yield return Case(Sign(date: null), "missing_auth_date");
        yield return Case(Sign(date: "private-invalid-date"), "invalid_auth_date");
        yield return Case(Sign(date: long.MaxValue.ToString(CultureInfo.InvariantCulture)), "invalid_timestamp");
        yield return Case(Sign(date: Now.AddMinutes(10).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)), "invalid_timestamp");
        yield return Case(Sign(date: Now.AddHours(-1).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)), "expired_timestamp", "Authentication.TelegramAuthExpired");
        yield return Case(Sign(user: null), "missing_user");
        yield return Case(Sign(user: "{private-malformed-user"), "invalid_user_json");
        yield return Case(Sign(user: "null"), "invalid_user");
        yield return Case(Sign(user: "{\"id\":0}"), "invalid_user_id");
        yield return Case(Sign(user: "{\"id\":-1}"), "invalid_user_id");
    }

    [Theory]
    [MemberData(nameof(Rejections))]
    public void RejectedProof_LogsOneSafeReasonAndPreservesError(string proof, string token, int ttl, string reason, string errorCode) {
        var logger = new TelegramOidcTestLogger<TelegramAuthValidator>();
        TelegramAuthValidator validator = CreateValidator(logger, token, ttl);

        ResultAssert.Failure(validator.ValidateInitData(proof), errorCode);

        Assert.True(logger.Entries.Count == 1, "Missing safe Mini App rejection diagnostic.");
        TelegramOidcTestLogger<TelegramAuthValidator>.Entry entry = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal(LogLevel.Information, entry.Level),
            () => Assert.Equal("init_data", entry.Fields["Stage"]),
            () => Assert.Equal(reason, entry.Fields["Reason"]),
            () => Assert.Null(entry.Exception),
            () => Assert.Equal(new[] { "Reason", "Stage", "{OriginalFormat}" }, entry.Fields.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal));

        string[] privateValues = [proof, BotToken, UserJson, PrivateName, PrivateField, "913579246", "secret-one", "secret-two", "private-invalid-date"];
        foreach (string value in privateValues.Where(value => !string.IsNullOrWhiteSpace(value))) {
            Assert.DoesNotContain(value, entry.Message, StringComparison.Ordinal);
            Assert.All(entry.Fields.Values, field => Assert.DoesNotContain(value, field?.ToString() ?? string.Empty, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ValidProof_PreservesIdentityWithoutLoggingRejection() {
        var logger = new TelegramOidcTestLogger<TelegramAuthValidator>();
        TelegramAuthValidator validator = CreateValidator(logger, BotToken, 3600);

        TelegramInitData identity = ResultAssert.Success(validator.ValidateInitData(Sign()));

        Assert.Multiple(
            () => Assert.Equal(913579246, identity.UserId),
            () => Assert.Equal(PrivateName, identity.FirstName),
            () => Assert.Equal(Now.UtcDateTime, identity.AuthDateUtc),
            () => Assert.Equal(Now.AddHours(1).UtcDateTime, identity.ExpiresAtUtc),
            () => Assert.Empty(logger.Entries));
    }

    private static object[] Case(string proof, string reason, string errorCode = "Authentication.TelegramInvalidData", string token = BotToken, int ttl = 3600) =>
        [proof, token, ttl, reason, errorCode];

    private static TelegramAuthValidator CreateValidator(ILogger<TelegramAuthValidator> logger, string token, int ttl) {
        using ServiceProvider provider = new ServiceCollection().AddSingleton(logger).BuildServiceProvider();
        return ActivatorUtilities.CreateInstance<TelegramAuthValidator>(provider,
            MsOptions.Create(new TelegramAuthOptions { BotToken = token, AuthTtlSeconds = ttl }), new FixedClock());
    }

    private static string Sign(string? date = "1793626800", string? user = UserJson) {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (date is not null) {
            fields["auth_date"] = date;
        }
        if (user is not null) {
            fields["user"] = user;
        }
        string dataCheck = string.Join('\n', fields.Select(field => $"{field.Key}={field.Value}"));
        byte[] secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(BotToken));
        string hash = Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(dataCheck)));
        return string.Join('&', fields.Select(field => $"{field.Key}={Uri.EscapeDataString(field.Value)}").Append("hash=" + hash));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}

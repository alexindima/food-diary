using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Services;

public sealed class TelegramAuthenticationIntentService(
    ITelegramLoginTicketStore tickets, IUserTelegramAccountService accounts, TimeProvider timeProvider) {
    internal const string LoginPurpose = "telegram-login";
    internal const string OnboardingPurpose = "telegram-onboarding";
    internal const string LinkPurpose = "telegram-link";

    public async Task<Result<TelegramAuthenticationIntentModel>> CreateAsync(
        long telegramUserId, string? firstName, string? lastName, string? language,
        string browserBinding, Guid? linkUserId, CancellationToken cancellationToken,
        string? oidcIssuer = null, string? oidcSubject = null) {
        Result<bool> registered = await accounts.IsRegisteredAsync(telegramUserId, cancellationToken).ConfigureAwait(false);
        if (registered.IsFailure) {
            return Result.Failure<TelegramAuthenticationIntentModel>(registered.Error);
        }
        (string purpose, string nextAction) = (linkUserId.HasValue, registered.Value) switch {
            (true, _) => (LinkPurpose, "link"),
            (false, true) => (LoginPurpose, "login"),
            _ => (OnboardingPurpose, "onboarding"),
        };
        var payload = new TelegramIntentPayload(telegramUserId, firstName, lastName, language, linkUserId, oidcIssuer, oidcSubject);
        DateTime expiresAtUtc = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(10);
        string ticket = await tickets.CreateAsync(purpose, browserBinding, JsonSerializer.Serialize(payload), expiresAtUtc, cancellationToken).ConfigureAwait(false);
        return Result.Success(new TelegramAuthenticationIntentModel(ticket, nextAction, expiresAtUtc));
    }

    internal sealed record TelegramIntentPayload(long TelegramUserId, string? FirstName, string? LastName, string? Language,
        Guid? LinkUserId, string? OidcIssuer = null, string? OidcSubject = null);
}

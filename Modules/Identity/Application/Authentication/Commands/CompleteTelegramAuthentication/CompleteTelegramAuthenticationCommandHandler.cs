using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramAuthentication;

public sealed class CompleteTelegramAuthenticationCommandHandler(
    ITelegramLoginTicketStore tickets,
    ITelegramIdentityPolicy policy,
    IUserTelegramAccountService accounts,
    IUserAuthenticationIdentityService identities,
    IAuthenticationTokenService tokens,
    TimeProvider timeProvider) : ICommandHandler<CompleteTelegramAuthenticationCommand, Result<AuthenticationModel>> {
    public async Task<Result<AuthenticationModel>> Handle(CompleteTelegramAuthenticationCommand command, CancellationToken cancellationToken) {
        if (!policy.LoginEnabled || (string.Equals(command.Action, "register", StringComparison.Ordinal) && !policy.RegistrationEnabled)) {
            return Result.Failure<AuthenticationModel>(TelegramIdentityErrors.NotConfigured);
        }
        string? purpose = command.Action switch {
            "login" => TelegramAuthenticationIntentService.LoginPurpose,
            "register" => TelegramAuthenticationIntentService.OnboardingPurpose,
            "link" when command.CurrentUserId.HasValue && command.CurrentUserId.Value != Guid.Empty => TelegramAuthenticationIntentService.LinkPurpose,
            _ => null,
        };
        if (purpose is null) {
            return Invalid();
        }
        if (string.Equals(command.Action, "register", StringComparison.Ordinal) && !IsValidTimeZone(command.TimeZoneId)) {
            return Result.Failure<AuthenticationModel>(TelegramIdentityErrors.TimeZoneRequired);
        }
        string? json = await tickets.ConsumeAsync(command.Ticket, purpose, command.BrowserBinding, cancellationToken).ConfigureAwait(false);
        if (json is null && string.Equals(command.Action, "link", StringComparison.Ordinal)) {
            json = await tickets.ConsumeAsync(command.Ticket, TelegramAuthenticationIntentService.OnboardingPurpose,
                command.BrowserBinding, cancellationToken).ConfigureAwait(false);
        }
        if (json is null) {
            return Invalid();
        }
        TelegramAuthenticationIntentService.TelegramIntentPayload? identity;
        try {
            identity = JsonSerializer.Deserialize<TelegramAuthenticationIntentService.TelegramIntentPayload>(json);
        } catch (JsonException) {
            return Invalid();
        }
        if (identity is null || identity.TelegramUserId <= 0 ||
            ((identity.OidcIssuer is null) != (identity.OidcSubject is null)) ||
            (identity.LinkUserId.HasValue && identity.LinkUserId != command.CurrentUserId)) {
            return Invalid();
        }
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Result<UserAuthenticationPrincipalModel> principal;
        if (string.Equals(command.Action, "register", StringComparison.Ordinal)) {
            principal = await accounts.RegisterAsync(new UserTelegramRegistrationModel(
                identity.TelegramUserId, identity.FirstName, identity.LastName,
                command.Language ?? identity.Language, command.TimeZoneId!, now, identity.OidcIssuer, identity.OidcSubject), cancellationToken).ConfigureAwait(false);
        } else if (string.Equals(command.Action, "link", StringComparison.Ordinal)) {
            var userId = new UserId(command.CurrentUserId!.Value);
            Result<UserModel> linked = await identities.LinkTelegramAsync(userId, identity.TelegramUserId, cancellationToken).ConfigureAwait(false);
            if (linked.IsFailure) {
                return Result.Failure<AuthenticationModel>(linked.Error);
            }
            principal = await identities.RecordAuthenticationAsync(userId, now, cancellationToken).ConfigureAwait(false);
        } else {
            principal = await identities.AuthenticateTelegramAsync(identity.TelegramUserId, now, cancellationToken).ConfigureAwait(false);
        }
        if (principal.IsFailure) {
            return Result.Failure<AuthenticationModel>(principal.Error);
        }
        if (!string.Equals(command.Action, "register", StringComparison.Ordinal) &&
            identity.OidcIssuer is not null && identity.OidcSubject is not null) {
            Result bound = await accounts.BindOidcIdentityAsync(principal.Value.UserId, identity.TelegramUserId,
                identity.OidcIssuer, identity.OidcSubject, cancellationToken).ConfigureAwait(false);
            if (bound.IsFailure) {
                return Result.Failure<AuthenticationModel>(bound.Error);
            }
        }
        IssuedAuthenticationTokens issued = await tokens.IssueFromPrincipalAsync(principal.Value, cancellationToken, command.ClientContext).ConfigureAwait(false);
        return Result.Success(new AuthenticationModel(issued.AccessToken, issued.RefreshToken, principal.Value.User));
    }

    private static bool IsValidTimeZone(string? timeZoneId) {
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId.Length > 100) {
            return false;
        }
        try {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return zone.HasIanaId || string.Equals(timeZoneId, "UTC", StringComparison.Ordinal);
        } catch (TimeZoneNotFoundException) {
            return false;
        } catch (InvalidTimeZoneException) {
            return false;
        }
    }

    private static Result<AuthenticationModel> Invalid() => Result.Failure<AuthenticationModel>(TelegramIdentityErrors.InvalidProof);
}

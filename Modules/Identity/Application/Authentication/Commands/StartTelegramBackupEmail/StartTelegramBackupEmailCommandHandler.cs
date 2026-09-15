using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Net.Mail;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramBackupEmail;

public sealed class StartTelegramBackupEmailCommandHandler(ITelegramOidcProvider provider, ITelegramLoginTicketStore tickets,
    IUserAuthenticationIdentityService identities, TimeProvider timeProvider) : ICommandHandler<StartTelegramBackupEmailCommand, Result<TelegramOidcStartModel>> {
    public async Task<Result<TelegramOidcStartModel>> Handle(StartTelegramBackupEmailCommand command, CancellationToken cancellationToken) {
        Guid userId = command.UserId ?? Guid.Empty;
        string email = command.Email;
        string browserBinding = command.BrowserBinding;
        if (userId == Guid.Empty || browserBinding.Length != 43 || string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
            !MailAddress.TryCreate(email, out MailAddress? address) || !string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase)) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.InvalidProof);
        }
        if (!provider.IsEnabled) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.NotConfigured);
        }
        Result<UserAuthenticationPrincipalModel> principal = await identities.GetAuthenticationPrincipalAsync(new UserId(userId), timeProvider.GetUtcNow().UtcDateTime, cancellationToken).ConfigureAwait(false);
        if (principal.IsFailure || principal.Value.Email is not null || !principal.Value.User.HasTelegramIdentity) {
            return Result.Failure<TelegramOidcStartModel>(TelegramIdentityErrors.InvalidProof);
        }
        var attempt = new TelegramBackupEmailOidcAttempt(userId, address.Address.Trim().ToLowerInvariant(), principal.Value.SecurityVersion,
            SecurityTokenGenerator.GenerateUrlSafeToken(), SecurityTokenGenerator.GenerateUrlSafeToken());
        string state = await tickets.CreateAsync(TelegramBackupEmailOidcAttempt.Purpose, TelegramBackupEmailOidcAttempt.Binding(browserBinding, userId), JsonSerializer.Serialize(attempt),
            timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5), cancellationToken).ConfigureAwait(false);
        Result<string> url = provider.CreateAuthorizationUrl(state, attempt.Nonce, attempt.CodeVerifier);
        return url.IsSuccess ? Result.Success(new TelegramOidcStartModel(url.Value)) : Result.Failure<TelegramOidcStartModel>(url.Error);
    }
}

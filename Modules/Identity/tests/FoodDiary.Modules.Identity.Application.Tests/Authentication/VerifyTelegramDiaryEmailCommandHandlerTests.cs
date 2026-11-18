using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.VerifyTelegramDiaryEmail;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class VerifyTelegramDiaryEmailCommandHandlerTests {
    [Theory]
    [InlineData("no_proof", "Authentication.TelegramDiaryProofExpired")]
    [InlineData("invalid_browser", "Authentication.TelegramDiaryProofExpired")]
    [InlineData("email_added", null)]
    [InlineData("account_unavailable", "User.NotFound")]
    [InlineData("target_missing", "User.EmailAlreadyExists")]
    [InlineData("same_account", "User.EmailAlreadyExists")]
    public async Task Verify_DoesNotStartATransferWithoutADifferentVerifiedAccount(string scenario, string? expectedError) {
        const string emailAddress = "diary@example.test";
        const long securityVersion = 7;
        var owner = UserId.New();
        ITelegramLoginTicketStore tickets = Substitute.For<ITelegramLoginTicketStore>();
        IUserTelegramAccountService accounts = Substitute.For<IUserTelegramAccountService>();
        IUserTelegramDiaryTransfer transfer = Substitute.For<IUserTelegramDiaryTransfer>();
        var email = new TelegramBackupEmailService(Substitute.For<ITelegramAuthValidator>(), Substitute.For<ITelegramAssertionReplayGuard>(),
            Substitute.For<IUserAuthenticationIdentityService>(), accounts, tickets, Substitute.For<IEmailSender>(), TimeProvider.System);
        string? proof = scenario is "no_proof" ? null :
            JsonSerializer.Serialize(new TelegramBackupEmailService.EmailProof(owner.Value, emailAddress, securityVersion));
        tickets.ConsumeAsync("proof", "telegram-backup-email", owner.Value.ToString("D"), CancellationToken.None).Returns(proof);
        accounts.AddVerifiedEmailAsync(owner, emailAddress, securityVersion, CancellationToken.None).Returns(scenario switch {
            "email_added" => Result.Success(),
            "account_unavailable" => Result.Failure(UserErrors.NotFound(owner)),
            _ => Result.Failure(UserErrors.EmailAlreadyExists),
        });
        transfer.FindTargetAsync(emailAddress, CancellationToken.None)
            .Returns(scenario is "same_account" ? new TelegramDiaryTarget(owner, securityVersion) : null);
        var command = new VerifyTelegramDiaryEmailCommand(owner.Value, TelegramBackupEmailService.TokenPrefix + "proof",
            new string('b', scenario is "invalid_browser" ? 42 : 43));
        var handler = new VerifyTelegramDiaryEmailCommandHandler(email, accounts, transfer, tickets, TimeProvider.System);

        Result<TelegramDiaryEmailVerification> result = await handler.Handle(command, CancellationToken.None);

        if (expectedError is null) {
            Assert.Null(ResultAssert.Success(result).TransferTicket);
        } else {
            Assert.Equal(expectedError, ResultAssert.Failure(result).Code);
        }
        await accounts.Received(scenario is "no_proof" or "invalid_browser" ? 0 : 1)
            .AddVerifiedEmailAsync(owner, emailAddress, securityVersion, CancellationToken.None);
        await transfer.Received(scenario is "target_missing" or "same_account" ? 1 : 0)
            .FindTargetAsync(emailAddress, CancellationToken.None);
        await tickets.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}

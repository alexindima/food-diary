using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.TransferTelegramDiary;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TransferTelegramDiaryCommandHandlerTests {
    [Theory]
    [InlineData("target_removed", "User.NotFound")]
    [InlineData("ticket_consumed", "Authentication.TelegramDiaryProofExpired")]
    [InlineData("pending_before_transfer", "Authentication.TelegramDiaryOperationPending")]
    [InlineData("pending_during_transfer", "Authentication.TelegramDiaryOperationPending")]
    public async Task Transfer_RequiresTheCurrentAccountProofAndAQuiescentTelegramDiary(string scenario, string expectedError) {
        var source = User.CreateTelegram(456, "hash");
        var target = User.Create("target@example.test", "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(target, DateTime.UtcNow);
        var attempt = new TelegramDiaryTransferAttempt(source.Id.Value, SourceSecurityVersion: 1, target.Id.Value, principal.SecurityVersion, "target@example.test");
        string payload = JsonSerializer.Serialize(attempt);
        var command = new TransferTelegramDiaryCommand(target.Id.Value, "ticket", "browser-binding", UseTelegramGoals: false);
        ITelegramLoginTicketStore tickets = Substitute.For<ITelegramLoginTicketStore>();
        IUserAuthenticationIdentityService identities = Substitute.For<IUserAuthenticationIdentityService>();
        ITelegramOperationStore operations = Substitute.For<ITelegramOperationStore>();
        IUserTelegramDiaryTransfer transfer = Substitute.For<IUserTelegramDiaryTransfer>();
        tickets.ReadAsync(command.Ticket, TelegramDiaryTransferAttempt.Purpose, command.BrowserBinding, CancellationToken.None)
            .Returns(payload);
        tickets.ConsumeAsync(command.Ticket, TelegramDiaryTransferAttempt.Purpose, command.BrowserBinding, CancellationToken.None)
            .Returns(scenario is "ticket_consumed" ? null : payload);
        identities.GetAuthenticationPrincipalAsync(target.Id, Arg.Any<DateTime>(), CancellationToken.None)
            .Returns(scenario is "target_removed"
                ? Result.Failure<UserAuthenticationPrincipalModel>(UserErrors.NotFound(target.Id))
                : Result.Success(principal));
        operations.HasPendingUserAsync(source.Id, CancellationToken.None)
            .Returns(scenario is "pending_before_transfer", scenario is "pending_during_transfer");
        bool transferAttempted = scenario is "pending_during_transfer";
        if (transferAttempted) {
            var goals = new TelegramDiaryGoals(Calories: 2000, Protein: null, Fat: null, Carbs: null, Fiber: null,
                Water: null, Steps: null, Weight: null, Waist: null);
            var preview = new TelegramDiaryPreview(new TelegramDiaryCounts(Meals: 1, Recipes: 0, Products: 0, WaterEntries: 0, Measurements: 0),
                goals, goals, CanUseTelegramGoals: true);
            transfer.TransferAsync(Arg.Any<TelegramDiaryProof>(), useTelegramGoals: false, CancellationToken.None).Returns(Result.Success(preview));
        }
        var handler = new TransferTelegramDiaryCommandHandler(tickets, transfer, operations, identities,
            new PrivilegedSessionService(Substitute.For<IRefreshTokenSessionReadRepository>(), TimeProvider.System), TimeProvider.System);

        Result<TelegramDiaryPreview> result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(expectedError, ResultAssert.Failure(result).Code);
        await transfer.Received(transferAttempted ? 1 : 0).TransferAsync(
            new TelegramDiaryProof(source.Id, SourceSecurityVersion: 1, target.Id, principal.SecurityVersion, "target@example.test", AdminTargetConfirmed: false),
            useTelegramGoals: false, CancellationToken.None);
        await operations.DidNotReceive().CancelUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }
}

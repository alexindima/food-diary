using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Commands.SetUserManualPremiumGrant;
using FoodDiary.Modules.Users.Contracts.Commands.SetUserManualPremiumGrant;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ManualPremiumGrantTests {
    [Theory]
    [InlineData("past")]
    [InlineData("blank-reason")]
    [InlineData("long-reason")]
    public async Task Grant_ValidatesExpirationAndReasonBeforeReadingOrWritingAccounts(string scenario) {
        IUserLookupRepository users = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writes = Substitute.For<IUserWriteRepository>();
        var handler = new SetUserManualPremiumGrantCommandHandler(users, writes, TimeProvider.System);
        DateTimeOffset end = DateTimeOffset.UtcNow.AddDays(scenario is "past" ? -1 : 1);
        string reason = scenario switch {
            "past" => "Owned manual-grant fixture",
            "long-reason" => new string('x', 501),
            _ => " ",
        };

        Result<UserManualPremiumGrantModel> result = await handler.Handle(
            new SetUserManualPremiumGrantCommand(UserId.New(), end, reason), CancellationToken.None);

        Error error = ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.NotNull(error.Details);
        Assert.Contains(scenario is "past" ? "endsAtUtc" : "reason", error.Details.Keys, StringComparer.Ordinal);
        Assert.Empty(users.ReceivedCalls());
        Assert.Empty(writes.ReceivedCalls());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("deleted")]
    [InlineData("system")]
    public async Task Grant_CannotChangeAnUnavailableAccount(string condition) {
        IUserLookupRepository users = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writes = Substitute.For<IUserWriteRepository>();
        var user = User.Create("deleted@example.test", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        UserId id = condition is "system" ? SystemContentOwner.Id : user.Id;
        if (condition is "deleted") {
            users.GetByIdIncludingDeletedAsync(id, CancellationToken.None).Returns(user);
        }
        var handler = new SetUserManualPremiumGrantCommandHandler(users, writes, TimeProvider.System);
        var request = new SetUserManualPremiumGrantCommand(id, DateTimeOffset.UtcNow.AddDays(1), "Manual test grant");

        Result<UserManualPremiumGrantModel> result = await handler.Handle(request, CancellationToken.None);

        Assert.Equal("User.NotFound", ResultAssert.Failure(result).Code);
        await users.Received(condition is "system" ? 0 : 1).GetByIdIncludingDeletedAsync(id, CancellationToken.None);
        await writes.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}

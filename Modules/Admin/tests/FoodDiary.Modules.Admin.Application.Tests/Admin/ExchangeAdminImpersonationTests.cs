using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Commands.ExchangeAdminImpersonation;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class ExchangeAdminImpersonationTests {
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("access-token")]
    public async Task Handle_OnlyReturnsNonBlankConsumedToken(string? token) {
        IAdminImpersonationHandoffService service = Substitute.For<IAdminImpersonationHandoffService>();
        using var cancellation = new CancellationTokenSource();
        service.ConsumeCodeAsync("one-time-code", cancellation.Token).Returns(token);
        var handler = new ExchangeAdminImpersonationCommandHandler(service);

        Result<string> result = await handler.Handle(new ExchangeAdminImpersonationCommand("one-time-code"), cancellation.Token);

        if (string.IsNullOrWhiteSpace(token)) {
            Assert.True(result.IsFailure);
            Assert.Equal(Errors.Authentication.InvalidToken.Code, result.Error.Code);
        } else {
            Assert.True(result.IsSuccess);
            Assert.Equal(token, result.Value);
        }
        await service.Received(1).ConsumeCodeAsync("one-time-code", cancellation.Token);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Validator_EnforcesCodeLengthBoundary(int length, bool valid) {
        var validator = new ExchangeAdminImpersonationCommandValidator();
        Assert.Equal(valid, validator.Validate(new ExchangeAdminImpersonationCommand(new string('a', length))).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Validator_RejectsMissingCode(string? code) {
        var validator = new ExchangeAdminImpersonationCommandValidator();
        Assert.False(validator.Validate(new ExchangeAdminImpersonationCommand(code!)).IsValid);
    }
}

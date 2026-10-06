using FluentValidation;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Results;

namespace FoodDiary.Application.Runtime.Tests;

[ExcludeFromCodeCoverage]
public sealed class ValidationFailureKindTests {
    [Theory]
    [InlineData(null, "EqualValidator", ErrorKind.Validation)]
    [InlineData("Recipe.UnknownInput", "Recipe.UnknownInput", ErrorKind.Validation)]
    [InlineData("Authentication.InvalidToken", "Authentication.InvalidToken", ErrorKind.Unauthorized)]
    [InlineData("Validation.Conflict", "Validation.Conflict", ErrorKind.Conflict)]
    [InlineData("Ai.OpenAiFailed", "Ai.OpenAiFailed", ErrorKind.ExternalFailure)]
    public async Task InvalidInput_PreservesCodeAndKnownKindOrDefaultsToValidation(string? customCode, string expectedCode, ErrorKind expectedKind) {
        var behavior = new ValidationBehavior<AmountCommand, Result>([new AmountValidator(customCode)]);
        bool handled = false;
        Result result = await behavior.Handle(new AmountCommand(100), _ => {
            handled = true;
            return Task.FromResult(Result.Success());
        }, CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal(expectedCode, result.Error.Code),
            () => Assert.Equal(expectedKind, result.Error.Kind),
            () => Assert.False(handled),
            () => Assert.Contains(nameof(AmountCommand.Amount), result.Error.Details!.Keys, StringComparer.Ordinal));
    }

    [Fact]
    public async Task ValidInput_ReachesHandlerWithItsCancellationToken() {
        var behavior = new ValidationBehavior<AmountCommand, Result>([new AmountValidator(customCode: null)]);
        using var cancellation = new CancellationTokenSource();
        bool handled = false;
        Result result = await behavior.Handle(new AmountCommand(0), token => {
            Assert.Equal(cancellation.Token, token);
            handled = true;
            return Task.FromResult(Result.Success());
        }, cancellation.Token);
        ResultAssert.Success(result);
        Assert.True(handled);
    }

    [ExcludeFromCodeCoverage]
    private sealed record AmountCommand(int Amount) : ICommand<Result>;

    [ExcludeFromCodeCoverage]
    private sealed class AmountValidator : AbstractValidator<AmountCommand> {
        public AmountValidator(string? customCode) {
            IRuleBuilderOptions<AmountCommand, int> rule = RuleFor(command => command.Amount).Equal(0);
            if (customCode is not null) {
                rule.WithErrorCode(customCode);
            }
        }
    }
}

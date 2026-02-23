using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Utilities;
using MediatR;

namespace FoodDiary.Application.Tests.Common;

public class CommonAbstractionsTests {
    [Fact]
    public void ResultFailure_WithGenericType_ThrowsOnValueAccess() {
        var result = Result.Failure<string>(Errors.Validation.Required("name"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public async Task ValidationBehavior_ForGenericResult_UsesDefaultValidationCode_WhenErrorCodeIsEmpty() {
        var validator = new GenericCommandValidator();
        var behavior = new ValidationBehavior<GenericCommand, Result<string>>([validator]);
        var command = new GenericCommand("");

        var result = await behavior.Handle(command, _ => Task.FromResult(Result.Success("ok")), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ValidationBehavior_ForNonGenericResult_ReturnsFailureResult() {
        var validator = new NonGenericCommandValidator();
        var behavior = new ValidationBehavior<NonGenericCommand, Result>([validator]);
        var command = new NonGenericCommand("");

        var result = await behavior.Handle(command, _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public void SecurityTokenGenerator_WithInvalidLength_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => SecurityTokenGenerator.GenerateUrlSafeToken(0));
    }

    [Fact]
    public void SecurityTokenGenerator_ReturnsUrlSafeToken() {
        var token = SecurityTokenGenerator.GenerateUrlSafeToken(32);

        Assert.NotEmpty(token);
        Assert.DoesNotContain("+", token, StringComparison.Ordinal);
        Assert.DoesNotContain("/", token, StringComparison.Ordinal);
        Assert.DoesNotContain("=", token, StringComparison.Ordinal);
    }

    private sealed record GenericCommand(string Value) : ICommand<Result<string>>;

    private sealed class GenericCommandValidator : AbstractValidator<GenericCommand> {
        public GenericCommandValidator() {
            RuleFor(x => x.Value)
                .Custom((_, context) => context.AddFailure(new ValidationFailure(
                    nameof(GenericCommand.Value),
                    "value is required")
                {
                    ErrorCode = " "
                }));
        }
    }

    private sealed record NonGenericCommand(string Value) : ICommand<Result>;

    private sealed class NonGenericCommandValidator : AbstractValidator<NonGenericCommand> {
        public NonGenericCommandValidator() {
            RuleFor(x => x.Value)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("value is required");
        }
    }
}

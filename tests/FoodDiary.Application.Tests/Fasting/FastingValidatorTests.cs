using FluentValidation.TestHelper;
using FoodDiary.Application.Fasting.Commands.StartFasting;

namespace FoodDiary.Application.Tests.Fasting;

public class FastingValidatorTests {
    private readonly StartFastingCommandValidator _validator = new();

    [Fact]
    public async Task StartFasting_WithNullUserId_HasError() {
        var command = new StartFastingCommand(null, "F16_8", null, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task StartFasting_WithEmptyUserId_HasError() {
        var command = new StartFastingCommand(Guid.Empty, "F16_8", null, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task StartFasting_WithEmptyProtocol_HasError() {
        var command = new StartFastingCommand(Guid.NewGuid(), "", null, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Protocol);
    }

    [Fact]
    public async Task StartFasting_WithValidCommand_NoErrors() {
        var command = new StartFastingCommand(Guid.NewGuid(), "F16_8", 16, null);
        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

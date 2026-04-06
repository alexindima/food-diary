using FluentValidation.TestHelper;
using FoodDiary.Application.ContentReports.Commands.CreateContentReport;

namespace FoodDiary.Application.Tests.ContentReports;

public class ContentReportsValidatorTests {
    private readonly CreateContentReportCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyTargetType_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "", Guid.NewGuid(), "spam");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.TargetType);
    }

    [Fact]
    public async Task Validate_WithInvalidTargetType_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Invalid", Guid.NewGuid(), "spam");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.TargetType);
    }

    [Fact]
    public async Task Validate_WithEmptyTargetId_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Recipe", Guid.Empty, "spam");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.TargetId);
    }

    [Fact]
    public async Task Validate_WithEmptyReason_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Recipe", Guid.NewGuid(), "");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public async Task Validate_WithTooLongReason_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Recipe", Guid.NewGuid(), new string('r', 1001));
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Theory]
    [InlineData("Recipe")]
    [InlineData("Comment")]
    public async Task Validate_WithValidCommand_NoErrors(string targetType) {
        var command = new CreateContentReportCommand(Guid.NewGuid(), targetType, Guid.NewGuid(), "Spam");
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

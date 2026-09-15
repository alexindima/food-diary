using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport;
using FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport;
using FoodDiary.Modules.ContentReports.Application.Commands.DismissContentReport;
using FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport;
using FluentValidation.TestHelper;
using FoodDiary.Modules.ContentReports.Application.Commands.CreateContentReport;

namespace FoodDiary.Modules.ContentReports.Application.Tests;

[ExcludeFromCodeCoverage]
public class ContentReportsValidatorTests {
    private readonly CreateContentReportCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptyTargetType_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "", Guid.NewGuid(), "spam");
        TestValidationResult<CreateContentReportCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.TargetType);
    }

    [Fact]
    public async Task Validate_WithInvalidTargetType_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Invalid", Guid.NewGuid(), "spam");
        TestValidationResult<CreateContentReportCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.TargetType);
    }

    [Fact]
    public async Task Validate_WithEmptyTargetId_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Recipe", Guid.Empty, "spam");
        TestValidationResult<CreateContentReportCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.TargetId);
    }

    [Fact]
    public async Task Validate_WithEmptyReason_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Recipe", Guid.NewGuid(), "");
        TestValidationResult<CreateContentReportCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public async Task Validate_WithTooLongReason_HasError() {
        var command = new CreateContentReportCommand(Guid.NewGuid(), "Recipe", Guid.NewGuid(), new string('r', 1001));
        TestValidationResult<CreateContentReportCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Theory]
    [InlineData("Recipe")]
    [InlineData("Comment")]
    public async Task Validate_WithValidCommand_NoErrors(string targetType) {
        var command = new CreateContentReportCommand(Guid.NewGuid(), targetType, Guid.NewGuid(), "Spam");
        TestValidationResult<CreateContentReportCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
    [Theory]
    [InlineData(-1, false, true)]
    [InlineData(0, false, true)]
    [InlineData(0, true, true)]
    [InlineData(1999, false, true)]
    [InlineData(2000, false, true)]
    [InlineData(2000, true, true)]
    [InlineData(2001, false, false)]
    [InlineData(2001, true, false)]
    public async Task Moderation_ValidatesNormalizedNoteLength(int length, bool padding, bool valid) {
        string? note = length < 0 ? null : new string('x', length);
        if (padding) {
            note = "  " + note + "  ";
        }
        var reportId = ContentReportId.New();
        var reviewerId = UserId.New();
        var review = new ReviewContentReportCommandValidator();
        var dismiss = new DismissContentReportCommandValidator();
        TestValidationResult<ReviewContentReportCommand> reviewResult = await review.TestValidateAsync(new ReviewContentReportCommand(reportId, reviewerId, note));
        TestValidationResult<DismissContentReportCommand> dismissResult = await dismiss.TestValidateAsync(new DismissContentReportCommand(reportId, reviewerId, note));
        if (valid) {
            reviewResult.ShouldNotHaveAnyValidationErrors();
            dismissResult.ShouldNotHaveAnyValidationErrors();
        } else {
            reviewResult.ShouldHaveValidationErrorFor(command => command.AdminNote).WithErrorCode("Validation.Invalid");
            dismissResult.ShouldHaveValidationErrorFor(command => command.AdminNote).WithErrorCode("Validation.Invalid");
        }
    }
}

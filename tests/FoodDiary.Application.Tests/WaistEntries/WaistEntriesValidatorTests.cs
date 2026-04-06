using FluentValidation.TestHelper;
using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

namespace FoodDiary.Application.Tests.WaistEntries;

public class WaistEntriesValidatorTests {
    [Fact]
    public async Task CreateWaist_WithNullUserId_HasError() {
        var result = await new CreateWaistEntryCommandValidator().TestValidateAsync(
            new CreateWaistEntryCommand(null, DateTime.UtcNow, 80));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateWaist_WithZeroCircumference_HasError() {
        var result = await new CreateWaistEntryCommandValidator().TestValidateAsync(
            new CreateWaistEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.Circumference);
    }

    [Fact]
    public async Task CreateWaist_WithValidData_NoErrors() {
        var result = await new CreateWaistEntryCommandValidator().TestValidateAsync(
            new CreateWaistEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 80.5));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteWaist_WithEmptyEntryId_HasError() {
        var result = await new DeleteWaistEntryCommandValidator().TestValidateAsync(
            new DeleteWaistEntryCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.WaistEntryId);
    }

    [Fact]
    public async Task UpdateWaist_WithZeroCircumference_HasError() {
        var result = await new UpdateWaistEntryCommandValidator().TestValidateAsync(
            new UpdateWaistEntryCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.Circumference);
    }

    [Fact]
    public async Task GetLatestWaist_WithNullUserId_HasError() {
        var result = await new GetLatestWaistEntryQueryValidator().TestValidateAsync(
            new GetLatestWaistEntryQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetWaistEntries_WithZeroLimit_HasError() {
        var result = await new GetWaistEntriesQueryValidator().TestValidateAsync(
            new GetWaistEntriesQuery(Guid.NewGuid(), null, null, 0, false));
        result.ShouldHaveValidationErrorFor(c => c.Limit);
    }

    [Fact]
    public async Task GetWaistEntries_WithInvertedDates_HasError() {
        var result = await new GetWaistEntriesQueryValidator().TestValidateAsync(
            new GetWaistEntriesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-7), null, false));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetWaistSummaries_WithZeroQuantization_HasError() {
        var result = await new GetWaistSummariesQueryValidator().TestValidateAsync(
            new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.QuantizationDays);
    }
}

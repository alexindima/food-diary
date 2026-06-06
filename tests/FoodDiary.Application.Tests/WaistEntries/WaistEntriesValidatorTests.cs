using FluentValidation.TestHelper;
using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

namespace FoodDiary.Application.Tests.WaistEntries;

[ExcludeFromCodeCoverage]
public class WaistEntriesValidatorTests {
    [Fact]
    public async Task CreateWaist_WithNullUserId_HasError() {
        TestValidationResult<CreateWaistEntryCommand> result = await new CreateWaistEntryCommandValidator().TestValidateAsync(
            new CreateWaistEntryCommand(UserId: null, DateTime.UtcNow, 80));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateWaist_WithZeroCircumference_HasError() {
        TestValidationResult<CreateWaistEntryCommand> result = await new CreateWaistEntryCommandValidator().TestValidateAsync(
            new CreateWaistEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.Circumference);
    }

    [Fact]
    public async Task CreateWaist_WithValidData_NoErrors() {
        TestValidationResult<CreateWaistEntryCommand> result = await new CreateWaistEntryCommandValidator().TestValidateAsync(
            new CreateWaistEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 80.5));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteWaist_WithEmptyEntryId_HasError() {
        TestValidationResult<DeleteWaistEntryCommand> result = await new DeleteWaistEntryCommandValidator().TestValidateAsync(
            new DeleteWaistEntryCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.WaistEntryId);
    }

    [Fact]
    public async Task UpdateWaist_WithZeroCircumference_HasError() {
        TestValidationResult<UpdateWaistEntryCommand> result = await new UpdateWaistEntryCommandValidator().TestValidateAsync(
            new UpdateWaistEntryCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.Circumference);
    }

    [Fact]
    public async Task GetLatestWaist_WithNullUserId_HasError() {
        TestValidationResult<GetLatestWaistEntryQuery> result = await new GetLatestWaistEntryQueryValidator().TestValidateAsync(
            new GetLatestWaistEntryQuery(UserId: null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetLatestWaist_WithEmptyUserId_HasError() {
        TestValidationResult<GetLatestWaistEntryQuery> result = await new GetLatestWaistEntryQueryValidator().TestValidateAsync(
            new GetLatestWaistEntryQuery(Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetLatestWaist_WithValidUserId_NoErrors() {
        TestValidationResult<GetLatestWaistEntryQuery> result = await new GetLatestWaistEntryQueryValidator().TestValidateAsync(
            new GetLatestWaistEntryQuery(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GetWaistEntries_WithZeroLimit_HasError() {
        TestValidationResult<GetWaistEntriesQuery> result = await new GetWaistEntriesQueryValidator().TestValidateAsync(
            new GetWaistEntriesQuery(Guid.NewGuid(), DateFrom: null, DateTo: null, 0, Descending: false));
        result.ShouldHaveValidationErrorFor(c => c.Limit);
    }

    [Fact]
    public async Task GetWaistEntries_WithInvertedDates_HasError() {
        TestValidationResult<GetWaistEntriesQuery> result = await new GetWaistEntriesQueryValidator().TestValidateAsync(
            new GetWaistEntriesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-7), Limit: null, Descending: false));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetWaistSummaries_WithZeroQuantization_HasError() {
        TestValidationResult<GetWaistSummariesQuery> result = await new GetWaistSummariesQueryValidator().TestValidateAsync(
            new GetWaistSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.QuantizationDays);
    }
}

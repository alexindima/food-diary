using FluentValidation.TestHelper;
using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;

namespace FoodDiary.Application.Tests.WeightEntries;

public class WeightEntriesValidatorTests {
    [Fact]
    public async Task CreateWeight_WithNullUserId_HasError() {
        var result = await new CreateWeightEntryCommandValidator().TestValidateAsync(
            new CreateWeightEntryCommand(null, DateTime.UtcNow, 75));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateWeight_WithZeroWeight_HasError() {
        var result = await new CreateWeightEntryCommandValidator().TestValidateAsync(
            new CreateWeightEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.Weight);
    }

    [Fact]
    public async Task CreateWeight_WithValidData_NoErrors() {
        var result = await new CreateWeightEntryCommandValidator().TestValidateAsync(
            new CreateWeightEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 75.5));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteWeight_WithEmptyEntryId_HasError() {
        var result = await new DeleteWeightEntryCommandValidator().TestValidateAsync(
            new DeleteWeightEntryCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.WeightEntryId);
    }

    [Fact]
    public async Task UpdateWeight_WithZeroWeight_HasError() {
        var result = await new UpdateWeightEntryCommandValidator().TestValidateAsync(
            new UpdateWeightEntryCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.Weight);
    }

    [Fact]
    public async Task GetLatestWeight_WithNullUserId_HasError() {
        var result = await new GetLatestWeightEntryQueryValidator().TestValidateAsync(
            new GetLatestWeightEntryQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetWeightEntries_WithZeroLimit_HasError() {
        var result = await new GetWeightEntriesQueryValidator().TestValidateAsync(
            new GetWeightEntriesQuery(Guid.NewGuid(), null, null, 0, false));
        result.ShouldHaveValidationErrorFor(c => c.Limit);
    }

    [Fact]
    public async Task GetWeightEntries_WithInvertedDates_HasError() {
        var result = await new GetWeightEntriesQueryValidator().TestValidateAsync(
            new GetWeightEntriesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-7), null, false));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetWeightSummaries_WithZeroQuantization_HasError() {
        var result = await new GetWeightSummariesQueryValidator().TestValidateAsync(
            new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.QuantizationDays);
    }

    [Fact]
    public async Task GetWeightSummaries_WithInvertedDates_HasError() {
        var result = await new GetWeightSummariesQueryValidator().TestValidateAsync(
            new GetWeightSummariesQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-7), 7));
        result.ShouldHaveValidationErrorFor(c => c.DateFrom);
    }
}

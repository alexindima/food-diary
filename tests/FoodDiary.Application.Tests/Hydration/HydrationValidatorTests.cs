using FluentValidation.TestHelper;
using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

namespace FoodDiary.Application.Tests.Hydration;

[ExcludeFromCodeCoverage]
public class HydrationValidatorTests {
    [Fact]
    public async Task CreateHydration_WithNullUserId_HasError() {
        TestValidationResult<CreateHydrationEntryCommand> result = await new CreateHydrationEntryCommandValidator().TestValidateAsync(
            new CreateHydrationEntryCommand(null, DateTime.UtcNow, 500));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateHydration_WithZeroAmount_HasError() {
        TestValidationResult<CreateHydrationEntryCommand> result = await new CreateHydrationEntryCommandValidator().TestValidateAsync(
            new CreateHydrationEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 0));
        result.ShouldHaveValidationErrorFor(c => c.AmountMl);
    }

    [Fact]
    public async Task CreateHydration_WithOverLimit_HasError() {
        TestValidationResult<CreateHydrationEntryCommand> result = await new CreateHydrationEntryCommandValidator().TestValidateAsync(
            new CreateHydrationEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 10001));
        result.ShouldHaveValidationErrorFor(c => c.AmountMl);
    }

    [Fact]
    public async Task CreateHydration_WithValidData_NoErrors() {
        TestValidationResult<CreateHydrationEntryCommand> result = await new CreateHydrationEntryCommandValidator().TestValidateAsync(
            new CreateHydrationEntryCommand(Guid.NewGuid(), DateTime.UtcNow, 500));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteHydration_WithEmptyEntryId_HasError() {
        TestValidationResult<DeleteHydrationEntryCommand> result = await new DeleteHydrationEntryCommandValidator().TestValidateAsync(
            new DeleteHydrationEntryCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.HydrationEntryId);
    }

    [Fact]
    public async Task UpdateHydration_WithNullUserId_HasError() {
        TestValidationResult<UpdateHydrationEntryCommand> result = await new UpdateHydrationEntryCommandValidator().TestValidateAsync(
            new UpdateHydrationEntryCommand(null, Guid.NewGuid(), null, null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateHydration_WithOverLimitAmount_HasError() {
        TestValidationResult<UpdateHydrationEntryCommand> result = await new UpdateHydrationEntryCommandValidator().TestValidateAsync(
            new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.NewGuid(), null, 10001));
        result.ShouldHaveValidationErrorFor(c => c.AmountMl);
    }

    [Fact]
    public async Task GetHydrationDailyTotal_WithNullUserId_HasError() {
        TestValidationResult<GetHydrationDailyTotalQuery> result = await new GetHydrationDailyTotalQueryValidator().TestValidateAsync(
            new GetHydrationDailyTotalQuery(null, DateTime.UtcNow));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetHydrationEntries_WithNullUserId_HasError() {
        TestValidationResult<GetHydrationEntriesQuery> result = await new GetHydrationEntriesQueryValidator().TestValidateAsync(
            new GetHydrationEntriesQuery(null, DateTime.UtcNow));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}

using FluentValidation.TestHelper;
using FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationEntry;
using FoodDiary.Modules.Hydration.Application.Commands.DeleteHydrationEntry;
using FoodDiary.Modules.Hydration.Application.Commands.UpdateHydrationEntry;
using FoodDiary.Modules.Hydration.Application.Queries.GetHydrationDailyTotal;
using FoodDiary.Modules.Hydration.Application.Queries.GetHydrationEntries;

namespace FoodDiary.Modules.Hydration.Application.Tests;

[ExcludeFromCodeCoverage]
public class HydrationValidatorTests {
    [Fact]
    public async Task CreateHydration_WithNullUserId_HasError() {
        TestValidationResult<CreateHydrationEntryCommand> result = await new CreateHydrationEntryCommandValidator().TestValidateAsync(
            new CreateHydrationEntryCommand(UserId: null, DateTime.UtcNow, 500));
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
            new UpdateHydrationEntryCommand(UserId: null, Guid.NewGuid(), TimestampUtc: null, AmountMl: null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpdateHydration_WithOverLimitAmount_HasError() {
        TestValidationResult<UpdateHydrationEntryCommand> result = await new UpdateHydrationEntryCommandValidator().TestValidateAsync(
            new UpdateHydrationEntryCommand(Guid.NewGuid(), Guid.NewGuid(), TimestampUtc: null, 10001));
        result.ShouldHaveValidationErrorFor(c => c.AmountMl);
    }

    [Fact]
    public async Task GetHydrationDailyTotal_WithNullUserId_HasError() {
        TestValidationResult<GetHydrationDailyTotalQuery> result = await new GetHydrationDailyTotalQueryValidator().TestValidateAsync(
            new GetHydrationDailyTotalQuery(UserId: null, DateTime.UtcNow));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetHydrationEntries_WithNullUserId_HasError() {
        TestValidationResult<GetHydrationEntriesQuery> result = await new GetHydrationEntriesQueryValidator().TestValidateAsync(
            new GetHydrationEntriesQuery(UserId: null, DateTime.UtcNow));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}

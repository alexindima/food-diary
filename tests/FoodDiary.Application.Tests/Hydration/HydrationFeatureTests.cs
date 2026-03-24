using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Hydration;

public class HydrationFeatureTests {
    [Fact]
    public async Task CreateHydrationEntryCommandValidator_WithEmptyUserId_Fails() {
        var validator = new CreateHydrationEntryCommandValidator();
        var command = new CreateHydrationEntryCommand(Guid.Empty, DateTime.UtcNow, 250);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DeleteHydrationEntryCommandValidator_WithEmptyEntryId_Fails() {
        var validator = new DeleteHydrationEntryCommandValidator();
        var command = new DeleteHydrationEntryCommand(Guid.NewGuid(), HydrationEntryId.Empty);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateHydrationEntryCommandValidator_WithInvalidAmount_Fails() {
        var validator = new UpdateHydrationEntryCommandValidator();
        var command = new UpdateHydrationEntryCommand(Guid.NewGuid(), HydrationEntryId.New(), DateTime.UtcNow, 0);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationDailyTotalQueryValidator_WithValidUserId_Passes() {
        var validator = new GetHydrationDailyTotalQueryValidator();
        var query = new GetHydrationDailyTotalQuery(Guid.NewGuid(), DateTime.UtcNow);

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetHydrationEntriesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetHydrationEntriesQueryValidator();
        var query = new GetHydrationEntriesQuery(Guid.Empty, DateTime.UtcNow);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void HydrationValidators_ValidateAmount_WithOutOfRangeValue_Fails(int amountMl) {
        var result = HydrationValidators.ValidateAmount(amountMl);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void HydrationValidators_ValidateAmount_WithValidValue_Passes() {
        var result = HydrationValidators.ValidateAmount(500);

        Assert.True(result.IsSuccess);
    }
}

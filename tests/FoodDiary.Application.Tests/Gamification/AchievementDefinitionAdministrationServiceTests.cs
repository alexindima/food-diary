using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Admin.Commands.CreateAdminAchievementDefinition;
using FoodDiary.Application.Admin.Commands.UpdateAdminAchievementDefinition;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Gamification;

[ExcludeFromCodeCoverage]
public sealed class AchievementDefinitionAdministrationServiceTests {
    [Fact]
    public async Task CreateAsync_WhenKeyAlreadyExists_ReturnsConflict() {
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.TryAddAsync(Arg.Any<AchievementDefinition>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        var service = new AchievementDefinitionAdministrationService(store);

        Result<AchievementDefinitionAdminModel> result = await service.CreateAsync(CreateInput(), CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal(ErrorKind.Conflict, result.Error.Kind),
            () => Assert.Equal("AchievementDefinition.KeyConflict", result.Error.Code));
    }

    [Fact]
    public async Task UpdateAsync_WhenVersionIsStale_ReturnsConflictWithoutMutation() {
        AchievementDefinition definition = CreateDefinition();
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.GetByIdTrackingAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var service = new AchievementDefinitionAdministrationService(store);

        Result<AchievementDefinitionAdminModel> result = await service.UpdateAsync(
            definition.Id.Value,
            UpdateInput(version: definition.Version + 1),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal(ErrorKind.Conflict, result.Error.Kind),
            () => Assert.Equal(1, definition.Version));
        await store.DidNotReceive().UpdateAsync(Arg.Any<AchievementDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithCurrentVersion_UpdatesAndIncrementsVersion() {
        AchievementDefinition definition = CreateDefinition();
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.GetByIdTrackingAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        var service = new AchievementDefinitionAdministrationService(store);

        Result<AchievementDefinitionAdminModel> result = await service.UpdateAsync(
            definition.Id.Value,
            UpdateInput(definition.Version),
            CancellationToken.None);

        AchievementDefinitionAdminModel model = ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(2, model.Version),
            () => Assert.Equal(20, model.Threshold),
            () => Assert.False(model.IsActive));
        await store.Received(1).UpdateAsync(definition, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateValidator_RejectsNullAndBoundaryViolations() {
        var validator = new CreateAdminAchievementDefinitionCommandValidator();
        AchievementDefinitionCreateInput invalid = CreateInput() with { Key = null!, Threshold = 0 };

        FluentValidation.Results.ValidationResult result = validator.Validate(new CreateAdminAchievementDefinitionCommand(invalid));

        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("Key", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("Threshold", StringComparison.Ordinal));
    }

    [Fact]
    public void UpdateValidator_RejectsEmptyIdAndVersion() {
        var validator = new UpdateAdminAchievementDefinitionCommandValidator();

        FluentValidation.Results.ValidationResult result = validator.Validate(
            new UpdateAdminAchievementDefinitionCommand(Guid.Empty, UpdateInput(version: 0)));

        Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, "Id", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("Version", StringComparison.Ordinal));
    }

    private static AchievementDefinitionCreateInput CreateInput() => new(
        "custom_20", "habits", nameof(AchievementMetric.TotalMeals), 20,
        "Название", "Title", "Описание", "Description", "trophy", SortOrder: 10, IsActive: true);

    private static AchievementDefinitionUpdateInput UpdateInput(int version) => new(
        "habits", nameof(AchievementMetric.TotalMeals), 20,
        "Название", "Title", "Описание", "Description", "trophy", SortOrder: 10, IsActive: false, Version: version);

    private static AchievementDefinition CreateDefinition() => AchievementDefinition.Create(
        "custom_10", "habits", AchievementMetric.TotalMeals, 10,
        "Название", "Title", "Описание", "Description", "trophy", 10);
}

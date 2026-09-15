using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Testing;
using FoodDiary.Modules.Gamification.Application.Commands.CreateAchievementDefinition;
using FoodDiary.Modules.Gamification.Contracts.Commands.CreateAchievementDefinition;
using FoodDiary.Modules.Gamification.Application.Commands.UpdateAchievementDefinition;
using FoodDiary.Modules.Gamification.Contracts.Commands.UpdateAchievementDefinition;
using FoodDiary.Modules.Gamification.Application.Queries.GetAchievementDefinitionsForAdministration;
using FoodDiary.Modules.Gamification.Contracts.Queries.GetAchievementDefinitionsForAdministration;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Modules.Admin.Application.Commands.CreateAdminAchievementDefinition;
using FoodDiary.Modules.Admin.Application.Commands.UpdateAdminAchievementDefinition;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Gamification.Application.Tests;

#pragma warning disable MA0003

[ExcludeFromCodeCoverage]
public sealed class AchievementDefinitionAdministrationServiceTests {
    [Fact]
    public async Task GetAllAsync_ReturnsMappedDefinitions() {
        AchievementDefinition definition = CreateDefinition();

        IAchievementDefinitionReadModelRepository reader = Substitute.For<IAchievementDefinitionReadModelRepository>();
        var expected = new AchievementDefinitionAdminModel(definition.Id.Value, definition.Key, definition.Category,
            definition.Metric.ToString(), definition.Threshold, definition.TitleRu, definition.TitleEn,
            definition.DescriptionRu, definition.DescriptionEn, definition.Icon, definition.SortOrder, definition.IsActive, definition.Version, 3);
        reader.GetForAdministrationAsync(Arg.Any<CancellationToken>()).Returns([expected]);
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(reader));
        AchievementDefinitionAdminModel model = Assert.Single(await service.Send(new GetAchievementDefinitionsForAdministrationQuery(), CancellationToken.None));
        Assert.Equal(3, model.AwardedUsers);

        Assert.Multiple(
            () => Assert.Equal(definition.Id.Value, model.Id),
            () => Assert.Equal(definition.Key, model.Key),
            () => Assert.Equal(nameof(AchievementMetric.TotalMeals), model.Metric));
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_AddsAndReturnsDefinition() {
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.TryAddAsync(Arg.Any<AchievementDefinition>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));

        AchievementDefinitionAdminModel model = ResultAssert.Success(
            await service.Send(new CreateAchievementDefinitionCommand(Input: CreateInput()), CancellationToken.None));

        Assert.Equal("custom_20", model.Key);
        await store.Received(1).TryAddAsync(Arg.Any<AchievementDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithUnknownMetric_ReturnsValidationFailure() {
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));

        Result<AchievementDefinitionAdminModel> result = await service.Send(new CreateAchievementDefinitionCommand(Input: CreateInput() with { Metric = "unknown" }), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await store.DidNotReceiveWithAnyArgs().TryAddAsync(default!, default);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDefinition_ReturnsValidationFailure() {
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(Substitute.For<IAchievementDefinitionStore>()), new UpdateAchievementDefinitionCommandHandler(Substitute.For<IAchievementDefinitionStore>()));

        Result<AchievementDefinitionAdminModel> result = await service.Send(new CreateAchievementDefinitionCommand(Input: CreateInput() with { Key = " " }), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
    }
    [Fact]
    public async Task CreateAsync_WhenKeyAlreadyExists_ReturnsConflict() {
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.TryAddAsync(Arg.Any<AchievementDefinition>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));

        Result<AchievementDefinitionAdminModel> result = await service.Send(new CreateAchievementDefinitionCommand(Input: CreateInput()), CancellationToken.None);

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
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));

        Result<AchievementDefinitionAdminModel> result = await service.Send(new UpdateAchievementDefinitionCommand(Id: definition.Id.Value, Input: UpdateInput(version: definition.Version + 1)), CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal(ErrorKind.Conflict, result.Error.Kind),
            () => Assert.Equal(1, definition.Version));
        await store.DidNotReceive().UpdateAsync(Arg.Any<AchievementDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenDefinitionDoesNotExist_ReturnsNotFound() {
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));
        var id = Guid.NewGuid();

        Result<AchievementDefinitionAdminModel> result = await service.Send(new UpdateAchievementDefinitionCommand(Id: id, Input: UpdateInput(1)), CancellationToken.None);

        Assert.Multiple(
            () => ResultAssert.Failure(result, "AchievementDefinition.NotFound"),
            () => Assert.Equal(ErrorKind.NotFound, result.Error.Kind),
            () => Assert.Contains(id.ToString(), result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownMetric_ReturnsValidationFailureWithoutMutation() {
        AchievementDefinition definition = CreateDefinition();
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.GetByIdTrackingAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));

        Result<AchievementDefinitionAdminModel> result = await service.Send(new UpdateAchievementDefinitionCommand(Id: definition.Id.Value, Input: UpdateInput(definition.Version) with { Metric = "unknown" }), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await store.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidDefinition_ReturnsValidationFailure() {
        AchievementDefinition definition = CreateDefinition();
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.GetByIdTrackingAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));

        Result<AchievementDefinitionAdminModel> result = await service.Send(new UpdateAchievementDefinitionCommand(Id: definition.Id.Value, Input: UpdateInput(definition.Version) with { Threshold = 0 }), CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
    }

    [Fact]
    public async Task UpdateAsync_WithCurrentVersion_UpdatesAndIncrementsVersion() {
        AchievementDefinition definition = CreateDefinition();
        IAchievementDefinitionStore store = Substitute.For<IAchievementDefinitionStore>();
        store.GetByIdTrackingAsync(definition.Id, Arg.Any<CancellationToken>()).Returns(definition);
        ISender service = RequestTestSender.Create(new GetAchievementDefinitionsForAdministrationQueryHandler(Substitute.For<IAchievementDefinitionReadModelRepository>()), new CreateAchievementDefinitionCommandHandler(store), new UpdateAchievementDefinitionCommandHandler(store));

        Result<AchievementDefinitionAdminModel> result = await service.Send(new UpdateAchievementDefinitionCommand(Id: definition.Id.Value, Input: UpdateInput(definition.Version)), CancellationToken.None);

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

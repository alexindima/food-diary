using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Commands.ArchiveRecommendationTemplate;
using FoodDiary.Application.Dietologist.Commands.CreateRecommendationTemplate;
using FoodDiary.Application.Dietologist.Commands.UpdateRecommendationTemplate;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Dietologist.Queries.SearchRecommendationTemplates;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

#pragma warning disable IDE0007, IDE0008, MA0003

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class RecommendationTemplateHandlerTests {
    [Fact]
    public async Task CreateRecommendationTemplate_CreatesMapsAndPersistsTemplate() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        IRecommendationTemplateRepository repository = Substitute.For<IRecommendationTemplateRepository>();
        var handler = new CreateRecommendationTemplateCommandHandler(
            repository,
            CreateUserContext(dietologist));

        Result<RecommendationTemplateModel> result = await handler.Handle(
            new CreateRecommendationTemplateCommand(dietologist.Id.Value, "  Breakfast  ", "  Add protein  "),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.NotEqual(Guid.Empty, result.Value.Id),
            () => Assert.Equal("Breakfast", result.Value.Name),
            () => Assert.Equal("Add protein", result.Value.Text),
            () => Assert.False(result.Value.IsArchived),
            () => Assert.NotEqual(default, result.Value.CreatedAtUtc),
            () => Assert.Null(result.Value.ModifiedAtUtc));
        await repository.Received(1).AddAsync(
            Arg.Is<RecommendationTemplate>(template =>
                template != null && template.DietologistUserId == dietologist.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRecommendationTemplate_WhenAccessFails_ReturnsFailure() {
        var handler = new CreateRecommendationTemplateCommandHandler(
            Substitute.For<IRecommendationTemplateRepository>(),
            CreateFailingUserContext());

        Result<RecommendationTemplateModel> result = await handler.Handle(
            new CreateRecommendationTemplateCommand(Guid.NewGuid(), "Name", "Text"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public void CreateRecommendationTemplateValidator_ValidatesShape() {
        var validator = new CreateRecommendationTemplateCommandValidator();
        var invalid = validator.TestValidate(
            new CreateRecommendationTemplateCommand(null, new string('x', 121), new string('x', 2001)));
        var empty = validator.TestValidate(
            new CreateRecommendationTemplateCommand(null, "", ""));
        var valid = validator.TestValidate(
            new CreateRecommendationTemplateCommand(null, new string('x', 120), new string('x', 2000)));

        Assert.Multiple(
            () => invalid.ShouldHaveValidationErrorFor(command => command.Name),
            () => invalid.ShouldHaveValidationErrorFor(command => command.Text),
            () => empty.ShouldHaveValidationErrorFor(command => command.Name),
            () => empty.ShouldHaveValidationErrorFor(command => command.Text),
            () => valid.ShouldNotHaveAnyValidationErrors());
    }

    [Fact]
    public async Task UpdateRecommendationTemplate_UpdatesOwnedTemplate() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        RecommendationTemplate template = RecommendationTemplate.Create(dietologist.Id, "Old", "Old text");
        var handler = new UpdateRecommendationTemplateCommandHandler(
            CreateRepository(template),
            CreateUserContext(dietologist));

        Result<RecommendationTemplateModel> result = await handler.Handle(
            new UpdateRecommendationTemplateCommand(
                dietologist.Id.Value,
                template.Id.Value,
                "  New  ",
                "  New text  "),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal("New", result.Value.Name),
            () => Assert.Equal("New text", result.Value.Text),
            () => Assert.NotNull(result.Value.ModifiedAtUtc));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task UpdateRecommendationTemplate_RejectsInvalidAccessIdOrOwnership(
        bool failUser,
        bool emptyId,
        bool foreignOwner) {
        User dietologist = User.Create("dietologist@example.com", "hash");
        RecommendationTemplate template = RecommendationTemplate.Create(
            foreignOwner ? UserId.New() : dietologist.Id,
            "Name",
            "Text");
        var handler = new UpdateRecommendationTemplateCommandHandler(
            CreateRepository(template),
            failUser ? CreateFailingUserContext() : CreateUserContext(dietologist));

        Result<RecommendationTemplateModel> result = await handler.Handle(
            new UpdateRecommendationTemplateCommand(
                dietologist.Id.Value,
                emptyId ? Guid.Empty : template.Id.Value,
                "New",
                "Text"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task UpdateRecommendationTemplate_WhenTemplateIsMissing_ReturnsNotFound() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        var handler = new UpdateRecommendationTemplateCommandHandler(
            Substitute.For<IRecommendationTemplateRepository>(),
            CreateUserContext(dietologist));

        Result<RecommendationTemplateModel> result = await handler.Handle(
            new UpdateRecommendationTemplateCommand(dietologist.Id.Value, Guid.NewGuid(), "New", "Text"),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Dietologist.InvitationNotFound.Code);
    }

    [Fact]
    public void UpdateRecommendationTemplateValidator_ValidatesShape() {
        var validator = new UpdateRecommendationTemplateCommandValidator();
        var invalid = validator.TestValidate(
            new UpdateRecommendationTemplateCommand(null, Guid.Empty, "", ""));
        var tooLong = validator.TestValidate(
            new UpdateRecommendationTemplateCommand(
                null,
                Guid.NewGuid(),
                new string('x', 121),
                new string('x', 2001)));
        var valid = validator.TestValidate(
            new UpdateRecommendationTemplateCommand(null, Guid.NewGuid(), "Name", "Text"));

        Assert.Multiple(
            () => invalid.ShouldHaveValidationErrorFor(command => command.TemplateId),
            () => invalid.ShouldHaveValidationErrorFor(command => command.Name),
            () => invalid.ShouldHaveValidationErrorFor(command => command.Text),
            () => tooLong.ShouldHaveValidationErrorFor(command => command.Name),
            () => tooLong.ShouldHaveValidationErrorFor(command => command.Text),
            () => valid.ShouldNotHaveAnyValidationErrors());
    }

    [Fact]
    public async Task ArchiveRecommendationTemplate_ArchivesOwnedTemplateIdempotently() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        RecommendationTemplate template = RecommendationTemplate.Create(dietologist.Id, "Name", "Text");
        var handler = new ArchiveRecommendationTemplateCommandHandler(
            CreateRepository(template),
            CreateUserContext(dietologist));

        Result first = await handler.Handle(
            new ArchiveRecommendationTemplateCommand(dietologist.Id.Value, template.Id.Value),
            CancellationToken.None);
        Result second = await handler.Handle(
            new ArchiveRecommendationTemplateCommand(dietologist.Id.Value, template.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(first);
        ResultAssert.Success(second);
        Assert.True(template.IsArchived);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task ArchiveRecommendationTemplate_RejectsInvalidAccessIdOrOwnership(
        bool failUser,
        bool emptyId,
        bool foreignOwner) {
        User dietologist = User.Create("dietologist@example.com", "hash");
        RecommendationTemplate template = RecommendationTemplate.Create(
            foreignOwner ? UserId.New() : dietologist.Id,
            "Name",
            "Text");
        var handler = new ArchiveRecommendationTemplateCommandHandler(
            CreateRepository(template),
            failUser ? CreateFailingUserContext() : CreateUserContext(dietologist));

        Result result = await handler.Handle(
            new ArchiveRecommendationTemplateCommand(
                dietologist.Id.Value,
                emptyId ? Guid.Empty : template.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task ArchiveRecommendationTemplate_WhenTemplateIsMissing_ReturnsNotFound() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        var handler = new ArchiveRecommendationTemplateCommandHandler(
            Substitute.For<IRecommendationTemplateRepository>(),
            CreateUserContext(dietologist));

        Result result = await handler.Handle(
            new ArchiveRecommendationTemplateCommand(dietologist.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result, Errors.Dietologist.InvitationNotFound.Code);
    }

    [Fact]
    public async Task RecommendationTemplateReadService_MapsRepositoryModels() {
        UserId dietologistId = UserId.New();
        IRecommendationTemplateRepository repository = Substitute.For<IRecommendationTemplateRepository>();
        DateTime createdAt = DateTime.UtcNow.AddDays(-2);
        DateTime modifiedAt = DateTime.UtcNow.AddDays(-1);
        repository.SearchAsync(dietologistId, "protein", true, Arg.Any<CancellationToken>())
            .Returns([
                new RecommendationTemplateReadModel(
                    Guid.NewGuid(),
                    "Protein",
                    "Add protein",
                    true,
                    createdAt,
                    modifiedAt),
            ]);
        var service = new RecommendationTemplateReadService(repository);

        IReadOnlyList<RecommendationTemplateModel> result = await service.SearchAsync(
            dietologistId,
            "protein",
            true,
            CancellationToken.None);

        RecommendationTemplateModel template = Assert.Single(result);
        Assert.Multiple(
            () => Assert.Equal("Protein", template.Name),
            () => Assert.Equal("Add protein", template.Text),
            () => Assert.True(template.IsArchived),
            () => Assert.Equal(createdAt, template.CreatedAtUtc),
            () => Assert.Equal(modifiedAt, template.ModifiedAtUtc));
    }

    [Fact]
    public async Task SearchRecommendationTemplates_ReturnsReadServiceResult() {
        User dietologist = User.Create("dietologist@example.com", "hash");
        IRecommendationTemplateReadService readService = Substitute.For<IRecommendationTemplateReadService>();
        RecommendationTemplateModel expected = new(
            Guid.NewGuid(),
            "Name",
            "Text",
            false,
            DateTime.UtcNow,
            null);
        readService.SearchAsync(
                dietologist.Id,
                "name",
                true,
                Arg.Any<CancellationToken>())
            .Returns([expected]);
        var handler = new SearchRecommendationTemplatesQueryHandler(
            readService,
            CreateUserContext(dietologist));

        Result<IReadOnlyList<RecommendationTemplateModel>> result = await handler.Handle(
            new SearchRecommendationTemplatesQuery(dietologist.Id.Value, "name", true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Same(expected, Assert.Single(result.Value));
    }

    [Fact]
    public async Task SearchRecommendationTemplates_WhenAccessFails_ReturnsFailure() {
        var handler = new SearchRecommendationTemplatesQueryHandler(
            Substitute.For<IRecommendationTemplateReadService>(),
            CreateFailingUserContext());

        Result<IReadOnlyList<RecommendationTemplateModel>> result = await handler.Handle(
            new SearchRecommendationTemplatesQuery(Guid.NewGuid(), null, false),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    private static IRecommendationTemplateRepository CreateRepository(RecommendationTemplate template) {
        IRecommendationTemplateRepository repository = Substitute.For<IRecommendationTemplateRepository>();
        repository.GetByIdAsync(template.Id, true, Arg.Any<CancellationToken>())
            .Returns(template);
        return repository;
    }

    private static IUserContextService CreateUserContext(User user) {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((Error?)null);
        service.GetAccessibleUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(user));
        return service;
    }

    private static IUserContextService CreateFailingUserContext() {
        IUserContextService service = Substitute.For<IUserContextService>();
        service.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Errors.Authentication.InvalidToken);
        return service;
    }
}

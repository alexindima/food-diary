using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Domain.Primitives;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Mediator;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Products.Infrastructure;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Application.Commands.CreateRecipe;
using FoodDiary.Modules.Recipes.Application.Commands.UpdateRecipe;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Contracts.Moderation;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Infrastructure;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.ReadModel.Composition;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed partial class RecipeModerationIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task StagedRecipeAndOutboxRowRollBackTogetherAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("moderation-author@example.com", "hash");
        database.Users.Add(author);
        await database.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, CreateProviderStub("OpenAi"));
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        var recipe = Recipe.Create(author.Id, "Rolled-back soup", 1, visibility: Visibility.Private);
        IRecipePublicationQueue queue = services.GetRequiredService<IRecipePublicationQueue>();
        Result transaction = await services.GetRequiredService<IRecipeMutationTransactionRunner>().ExecuteAsync(async token => {
            await services.GetRequiredService<IRecipeWriteRepository>().AddAsync(recipe, token);
            await queue.StageAsync(recipe.Id, author.Id, "{}", new RecipeModerationContent(recipe.Name, []), token);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(token);
            return Result.Failure(new Error("Recipes.TestRollback", "Forced rollback after both writes.", ErrorKind.Conflict));
        });

        Assert.Equal("Recipes.TestRollback", transaction.Error.Code);
        Assert.False(await database.Recipes.AnyAsync(candidate => candidate.Id == recipe.Id));
        Assert.Null(await queue.GetCurrentAsync(recipe.Id, CancellationToken.None));
    }

    [RequiresDockerFact]
    public async Task PublicRecipeWaitsForReviewUsesPinnedRulesAndSkipsUnconfiguredMistralAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("moderation-author@example.com", "hash");
        var reader = User.Create("moderation-reader@example.com", "hash");
        database.Users.AddRange(author, reader);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: true));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi);

        RecipeModerationPolicyView first = await PublishPolicyAsync(provider, author.Id, "harassment");
        RecipeModel pending = await CreatePendingAsync(provider, author.Id);
        Guid revisionId = GetRevisionId(pending);
        var recipeId = new RecipeId(pending.Id);
        RecipeModerationPolicyView second = await PublishPolicyAsync(provider, author.Id, "hate");
        RecipeModerationItem before = await GetItemAsync(provider, revisionId);

        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Pending, pending.PublicationStatus),
            () => Assert.Equal(first.Active.Id, before.PolicyVersionId),
            () => Assert.NotEqual(first.Active.Id, second.Active.Id),
            () => Assert.False(first.MistralAvailable));
        Assert.Null(await ReadRecipeAsync(provider, recipeId, reader.Id));
        Assert.False(await ProcessAsync(provider, observation: true));
        Assert.True(await ProcessAsync(provider));

        RecipeModerationItem review = await GetItemAsync(provider, revisionId);
        Assert.Equal(RecipePublicationStatuses.NeedsReview, review.Status);
        Assert.NotNull(review.PrimaryResult);
        Assert.Null(review.ShadowResult);
        Assert.Null(await ReadRecipeAsync(provider, recipeId, reader.Id));

        await using (AsyncServiceScope scope = provider.CreateAsyncScope()) {
            IRecipeModerationAdministration moderation = scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>();
            Assert.Equal(RecipeModerationActionOutcome.Applied, await moderation.ResolveAsync(
                revisionId, author.Id.Value, approve: true, reason: null, CancellationToken.None));
        }
        Assert.True(await ProcessAsync(provider));
        Assert.Equal(RecipePublicationStatuses.Published, (await GetItemAsync(provider, revisionId)).Status);
        Assert.Equal("Tomato soup", (await ReadRecipeAsync(provider, recipeId, reader.Id))?.Name);
    }

    [RequiresDockerFact]
    public async Task MistralObservationFlagAndFailureDoNotChangeOpenAiPublicationAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("moderation-author@example.com", "hash");
        var reader = User.Create("moderation-reader@example.com", "hash");
        database.Users.AddRange(author, reader);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(Check(flagged: false));
        IRecipeModerationProvider mistral = CreateProviderStub("Mistral");
        mistral.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromResult(Check(flagged: true, model: "mistral-fixture", category: "hate_and_discrimination")),
            _ => Task.FromException<RecipeModerationCheck>(
                new RecipeModerationProviderException("Synthetic observation failure.", retryable: false)));
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, mistral);
        RecipeModerationPolicyView policy = await PublishPolicyAsync(provider, author.Id, "harassment");
        Assert.True(policy.MistralAvailable);

        foreach (bool expectsShadowResult in new[] { true, false }) {
            RecipeModel pending = await CreatePendingAsync(provider, author.Id);
            Guid revisionId = GetRevisionId(pending);
            Assert.True(await ProcessAsync(provider, observation: true));
            RecipeModerationItem observed = await GetItemAsync(provider, revisionId);
            Assert.Equal(RecipePublicationStatuses.Pending, observed.Status);
            Assert.Null(observed.PrimaryResult);
            if (expectsShadowResult) {
                Assert.NotNull(observed.ShadowResult);
                Assert.Contains("hate_and_discrimination", observed.ShadowResult, StringComparison.Ordinal);
            } else {
                Assert.Null(observed.ShadowResult);
            }
            Assert.False(await ProcessAsync(provider, observation: true));

            Assert.True(await ProcessAsync(provider));
            Assert.True(await ProcessAsync(provider));
            RecipeModerationItem published = await GetItemAsync(provider, revisionId);
            Assert.Equal(RecipePublicationStatuses.Published, published.Status);
            Assert.Equal(observed.ShadowResult, published.ShadowResult);
            Assert.Equal("Tomato soup", (await ReadRecipeAsync(provider, new RecipeId(pending.Id), reader.Id))?.Name);
        }

        await mistral.Received(2).CheckAsync(Arg.Is<RecipeModerationContent>(content => content.Images.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [RequiresDockerFact]
    public async Task LateClearCheckCannotPublishAfterOwnerSupersedesTheClaimAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var author = User.Create("moderation-author@example.com", "hash");
        var reader = User.Create("moderation-reader@example.com", "hash");
        database.Users.AddRange(author, reader);
        await database.SaveChangesAsync();
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi);
        RecipeModel first = await CreatePendingAsync(provider, author.Id);
        Guid firstRevisionId = GetRevisionId(first);
        Guid replacementRevisionId = Guid.Empty;
        openAi.CheckAsync(
                Arg.Is<RecipeModerationContent>(content => content.Text.StartsWith("Tomato soup", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(async _ => {
                RecipeModel replacement = await ReplacePendingAsync(provider, author.Id, first.Id, "Bean soup");
                replacementRevisionId = GetRevisionId(replacement);
                return Check(flagged: false);
            });
        openAi.CheckAsync(
            Arg.Is<RecipeModerationContent>(content => content.Text.StartsWith("Bean soup", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).Returns(Check(flagged: false));

        Assert.True(await ProcessAsync(provider));
        Assert.NotEqual(Guid.Empty, replacementRevisionId);
        var recipeId = new RecipeId(first.Id);
        RecipeModerationItem stale = await GetItemAsync(provider, firstRevisionId);
        Assert.Equal(RecipePublicationStatuses.Superseded, stale.Status);
        Assert.Null(stale.PrimaryResult);
        Assert.Equal(RecipePublicationStatuses.Pending, (await GetItemAsync(provider, replacementRevisionId)).Status);
        Assert.Null(await ReadRecipeAsync(provider, recipeId, reader.Id));
        Assert.True(await ProcessAsync(provider));
        Assert.True(await ProcessAsync(provider));
        Assert.Equal(RecipePublicationStatuses.Published, (await GetItemAsync(provider, replacementRevisionId)).Status);
        Assert.Equal("Bean soup", (await ReadRecipeAsync(provider, recipeId, reader.Id))?.Name);

        RecipeModel edit = await ReplacePendingAsync(provider, author.Id, first.Id, "Pea soup");
        Assert.Equal(RecipePublicationStatuses.Pending, edit.PublicationStatus);
        Assert.Equal("Pea soup", edit.Name);
        Assert.Equal("Bean soup", (await ReadRecipeAsync(provider, recipeId, reader.Id))?.Name);
    }

    private static async Task<RecipeModel> CreatePendingAsync(ServiceProvider provider, UserId owner) {
        var command = new CreateRecipeCommand(
            owner.Value, "Tomato soup", Description: null, Comment: null, Category: null, ImageUrl: null,
            ImageAssetId: null, PrepTime: null, CookTime: null, Servings: 2, nameof(Visibility.Public),
            CalculateNutritionAutomatically: true, ManualCalories: null, ManualProteins: null, ManualFats: null,
            ManualCarbs: null, ManualFiber: null, ManualAlcohol: null, Steps: CreateSteps()) { Language = "en" };
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        return ResultAssert.Success(await scope.ServiceProvider.GetRequiredService<IRequestHandler<CreateRecipeCommand, Result<RecipeModel>>>()
            .Handle(command, CancellationToken.None));
    }

    private static async Task<RecipeModel> ReplacePendingAsync(
        ServiceProvider provider, UserId owner, Guid recipeId, string name, IReadOnlyList<Guid>? imageIds = null) {
        var command = new UpdateRecipeCommand(
            owner.Value, recipeId, name, Description: null, ClearDescription: false, Comment: null, ClearComment: false,
            Category: null, ClearCategory: false, ImageUrl: null, ClearImageUrl: false, ImageAssetId: null, ClearImageAssetId: false,
            PrepTime: null, CookTime: null, Servings: null, nameof(Visibility.Public), CalculateNutritionAutomatically: true,
            ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
            Steps: CreateSteps()) { Language = "en", ImageAssetIds = imageIds };
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        return ResultAssert.Success(await scope.ServiceProvider.GetRequiredService<IRequestHandler<UpdateRecipeCommand, Result<RecipeModel>>>()
            .Handle(command, CancellationToken.None));
    }

    private static async Task<RecipeModerationPolicyView> PublishPolicyAsync(ServiceProvider provider, UserId actor, string category) {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>()
            .PublishPolicyAsync(actor.Value, new RecipeModerationPolicyInput([category], ["violence"], MistralSamplePercent: 100), CancellationToken.None);
    }

    private static async Task<RecipeModerationItem> GetItemAsync(ServiceProvider provider, Guid revisionId) {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        RecipeModerationItem? item = await scope.ServiceProvider.GetRequiredService<IRecipeModerationAdministration>()
            .GetAsync(revisionId, CancellationToken.None);
        return Assert.IsType<RecipeModerationItem>(item);
    }

    private static async Task<Recipe?> ReadRecipeAsync(ServiceProvider provider, RecipeId recipeId, UserId viewer) {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IRecipeReadRepository>()
            .GetByIdAsync(recipeId, viewer, includePublic: true, cancellationToken: CancellationToken.None);
    }

    private static async Task<bool> ProcessAsync(
        ServiceProvider provider, bool observation = false, CancellationToken cancellationToken = default) {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IRecipeModerationProcessor>()
            .ProcessNextAsync(observation, cancellationToken);
    }

    private static Guid GetRevisionId(RecipeModel model) {
        Assert.NotNull(model.PublicationRevisionId);
        return model.PublicationRevisionId.GetValueOrDefault();
    }

    private static IReadOnlyList<RecipeStepInput> CreateSteps() => [
        new RecipeStepInput(1, "Cook and serve.", Title: null, ImageUrl: null, ImageAssetId: null, Ingredients: []),
    ];

    private static IRecipeModerationProvider CreateProviderStub(string name) {
        IRecipeModerationProvider provider = Substitute.For<IRecipeModerationProvider>();
        provider.Name.Returns(name);
        provider.IsAvailable.Returns(returnThis: true);
        return provider;
    }

    private static RecipeModerationCheck Check(bool flagged, string model = "fixture-model", string category = "harassment") => new(model, [
        new RecipeModerationSignal("text", "fixture-request",
            new Dictionary<string, bool>(StringComparer.Ordinal) { [category] = flagged },
            new Dictionary<string, double>(StringComparer.Ordinal) { [category] = flagged ? 0.9 : 0.01 },
            flagged),
    ]);

    private static ServiceProvider CreateProvider(
        string connectionString,
        IRecipeModerationProvider openAi,
        IRecipeModerationProvider? mistral = null,
        IRecipeImagePromotionService? photoPromotion = null,
        IReadOnlyList<ImageAsset>? privateAssets = null,
        TimeProvider? clock = null,
        IImageAssetContentService? imageContent = null) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["RecipeModeration:Enabled"] = "true",
            ["Mistral:ApiKey"] = mistral is null ? null : "fixture-observation-key",
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddInfrastructure(configuration).AddOutboxProcessing(configuration)
            .AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(Substitute.For<IDomainEventPublisher>());
        services.AddSingleton(Substitute.For<ICurrentUserAccessService>());
        services.AddSingleton(Substitute.For<IUserProfileReadService>());
        IImageAssetAccessService imageAccess = Substitute.For<IImageAssetAccessService>();
        imageAccess.ResolveOptionalAsync(assetId: null, Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<ImageAssetReadModel?>(value: null));
        foreach (ImageAsset asset in privateAssets ?? []) {
            imageAccess.ResolveOptionalAsync(asset.Id, asset.UserId, Arg.Any<CancellationToken>())
                .Returns(Result.Success<ImageAssetReadModel?>(new ImageAssetReadModel(asset.Id, asset.Url, IsPrivate: true)));
        }
        services.AddSingleton(imageAccess);
        services.AddSingleton(photoPromotion ?? Substitute.For<IRecipeImagePromotionService>());
        services.AddSingleton(imageContent ?? Substitute.For<IImageAssetContentService>());
        services.AddSingleton(Substitute.For<IImageAssetCleanupService>());
        services.AddSingleton(openAi);
        if (mistral is not null) {
            services.AddSingleton(mistral);
        }
        services.AddProductsPersistence();
        services.AddRecipesModule().AddRecipePublicationWorker(configuration);
        services.AddReadModelComposition();
        if (clock is not null) {
            services.AddSingleton(clock);
        }
        return services.BuildServiceProvider();
    }
}

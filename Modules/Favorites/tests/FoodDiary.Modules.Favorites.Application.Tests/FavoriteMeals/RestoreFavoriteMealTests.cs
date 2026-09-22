using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Commands.RestoreFavoriteMeal;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.Tests.FavoriteMeals;

[ExcludeFromCodeCoverage]
public sealed class RestoreFavoriteMealTests {
    private readonly UserId _owner = UserId.New();
    private readonly IFavoriteMealWriteRepository _repository = Substitute.For<IFavoriteMealWriteRepository>();
    private readonly IFavoriteMealSourceReadService _source = Substitute.For<IFavoriteMealSourceReadService>();
    private readonly ICurrentUserAccessService _access = Substitute.For<ICurrentUserAccessService>();

    [Fact]
    public async Task RestoresSameRecordAndRepeatedRequestIsIdempotentAsync() {
        FavoriteMeal favorite = ArrangeRemoved();
        using var cancellation = new CancellationTokenSource();
        var handler = new RestoreFavoriteMealCommandHandler(_repository, _source, _access);
        var command = new RestoreFavoriteMealCommand(_owner.Value, favorite.Id.Value);
        FavoriteMealModel result = ResultAssert.Success(await handler.Handle(command, cancellation.Token));
        ResultAssert.Success(await handler.Handle(command, cancellation.Token));
        Assert.Multiple(
            () => Assert.Null(favorite.RemovedAtUtc),
            () => Assert.Equal(favorite.Id.Value, result.Id),
            () => Assert.Equal(favorite.CreatedAtUtc, result.CreatedAtUtc),
            () => Assert.Equal("Lunch", result.Name));
        await _repository.Received(2).GetForRestoreAsync(favorite.Id, _owner, cancellation.Token);
        await _source.Received(2).GetAccessibleAsync(_owner, favorite.MealId, cancellation.Token);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task RejectsMissingOrForeignFavoriteAsync() {
        var id = FavoriteMealId.New();
        var handler = new RestoreFavoriteMealCommandHandler(_repository, _source, _access);
        ResultAssert.Failure(await handler.Handle(new RestoreFavoriteMealCommand(_owner.Value, id.Value), CancellationToken.None));
        await _repository.Received(1).GetForRestoreAsync(id, _owner, CancellationToken.None);
        Assert.Empty(_source.ReceivedCalls());
    }

    [Fact]
    public async Task DeniedOwnerCannotReadOrRestoreAsync() {
        _access.EnsureCanAccessAsync(_owner, Arg.Any<CancellationToken>()).Returns(AuthenticationErrors.InvalidToken);
        var handler = new RestoreFavoriteMealCommandHandler(_repository, _source, _access);
        ResultAssert.Failure(await handler.Handle(new RestoreFavoriteMealCommand(_owner.Value, Guid.NewGuid()), CancellationToken.None));
        Assert.Empty(_repository.ReceivedCalls());
    }

    [Fact]
    public async Task EmptyIdentifierDoesNotReadRepositoryAsync() {
        var handler = new RestoreFavoriteMealCommandHandler(_repository, _source, _access);
        ResultAssert.Failure(await handler.Handle(new RestoreFavoriteMealCommand(_owner.Value, Guid.Empty), CancellationToken.None));
        Assert.Empty(_repository.ReceivedCalls());
    }

    [Fact]
    public async Task InaccessibleSourceLeavesFavoriteRemovedAsync() {
        FavoriteMeal favorite = ArrangeRemoved();
        _source.GetAccessibleAsync(_owner, favorite.MealId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<FavoriteMealSourceModel>(AuthenticationErrors.InvalidToken));
        var handler = new RestoreFavoriteMealCommandHandler(_repository, _source, _access);
        ResultAssert.Failure(await handler.Handle(new RestoreFavoriteMealCommand(_owner.Value, favorite.Id.Value), CancellationToken.None));
        Assert.NotNull(favorite.RemovedAtUtc);
    }

    [Fact]
    public async Task NewFavoriteForSameMealIsNotOverwrittenAsync() {
        FavoriteMeal favorite = ArrangeRemoved();
        var replacement = FavoriteMeal.Create(_owner, favorite.MealId, "New name");
        _repository.GetByMealIdAsync(favorite.MealId, _owner, Arg.Any<CancellationToken>()).Returns(replacement);
        var handler = new RestoreFavoriteMealCommandHandler(_repository, _source, _access);
        Result<FavoriteMealModel> result = await handler.Handle(new RestoreFavoriteMealCommand(_owner.Value, favorite.Id.Value), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal(FavoriteMealErrors.AlreadyExists, result.Error);
        Assert.NotNull(favorite.RemovedAtUtc);
        Assert.Equal("New name", replacement.Name);
    }

    private FavoriteMeal ArrangeRemoved() {
        var favorite = FavoriteMeal.Create(_owner, MealId.New(), "Lunch");
        favorite.Remove();
        _repository.GetForRestoreAsync(favorite.Id, _owner, Arg.Any<CancellationToken>()).Returns(favorite);
        _source.GetAccessibleAsync(_owner, favorite.MealId, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new FavoriteMealSourceModel(DateTime.UtcNow, "Lunch", 500, 20, 10, 30, 2)));
        return favorite;
    }
}

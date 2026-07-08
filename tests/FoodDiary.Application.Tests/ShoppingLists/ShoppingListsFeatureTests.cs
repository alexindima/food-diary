using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public partial class ShoppingListsFeatureTests {
    private static ShoppingListReadModel ToReadModel(ShoppingList list) =>
        new(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            [.. list.Items
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Id.Value)
                .Select(item => new ShoppingListItemReadModel(
                    item.Id.Value,
                    item.ShoppingListId.Value,
                    item.ProductId?.Value,
                    item.Name,
                    item.Amount,
                    item.Unit?.ToString(),
                    item.Category,
                    item.Aisle,
                    item.Note,
                    item.IsChecked,
                    item.CheckedOnUtc,
                    item.SortOrder,
                    [.. item.Sources
                        .OrderBy(source => source.DayNumber ?? int.MaxValue)
                        .ThenBy(source => source.Label, StringComparer.Ordinal)
                        .Select(source => new ShoppingListItemSourceReadModel(
                            source.Id.Value,
                            source.SourceType.ToString(),
                            source.MealPlanId?.Value,
                            source.MealPlanMealId?.Value,
                            source.RecipeId?.Value,
                            source.Label,
                            source.DayNumber,
                            source.MealType,
                            source.Amount,
                            source.Unit?.ToString()))]))]);

    private static ShoppingListSummaryReadModel ToSummaryReadModel(ShoppingList list) =>
        new(list.Id.Value, list.Name, list.CreatedOnUtc, list.Items.Count);








































    [ExcludeFromCodeCoverage]
    private sealed class NoopShoppingListRepository : IShoppingListRepository {
        public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.FromResult(list);

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingListReadModel?> GetReadModelByIdAsync(ShoppingListId id, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<ShoppingListReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ShoppingList>>([]);

        public Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingListSummaryReadModel>>([]);

        public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingShoppingListRepository : IShoppingListRepository {
        public ShoppingList? AddedList { get; private set; }

        public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) {
            AddedList = list;
            return Task.FromResult(list);
        }

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingListReadModel?> GetReadModelByIdAsync(ShoppingListId id, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<ShoppingListReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ShoppingList>>([]);

        public Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingListSummaryReadModel>>([]);

        public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleShoppingListRepository(ShoppingList list) : IShoppingListRepository {
        public bool UpdateCalled { get; private set; }
        public bool DeleteCalled { get; private set; }

        public Task<ShoppingList> AddAsync(ShoppingList addedList, CancellationToken cancellationToken = default) =>
            Task.FromResult(addedList);

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingList?>(id == list.Id && userId == list.UserId ? list : null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingList?>(userId == list.UserId ? list : null);

        public Task<ShoppingListReadModel?> GetReadModelByIdAsync(ShoppingListId id, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == list.Id && userId == list.UserId ? ToReadModel(list) : null);

        public Task<ShoppingListReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == list.UserId ? ToReadModel(list) : null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingList>>(userId == list.UserId ? [list] : []);

        public Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingListSummaryReadModel>>(userId == list.UserId ? [ToSummaryReadModel(list)] : []);

        public Task UpdateAsync(ShoppingList updatedList, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ShoppingList deletedList, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
    }

    private static IProductLookupService CreateThrowingProductLookupService() {
        IProductLookupService service = Substitute.For<IProductLookupService>();
        service
            .GetAccessibleByIdsAsync(Arg.Any<IEnumerable<ProductId>>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyDictionary<ProductId, Product>>>(_ => throw new InvalidOperationException("Product lookup should not be called."));

        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ProductLookupService(params Product[] products) : IProductLookupService {
        private readonly Dictionary<ProductId, Product> _products = products.ToDictionary(product => product.Id);

        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(
                ids.Where(_products.ContainsKey).ToDictionary(id => id, id => _products[id]));
    }


    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                Error? error = user switch {
                    { Id: var id } when id != userId => Errors.Authentication.InvalidToken,
                    { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                    _ => null,
                };
                return Task.FromResult(error);
            });

        return service;
    }

    private static GetCurrentShoppingListQueryHandler CreateCurrentShoppingListHandler(
        IShoppingListReadModelRepository shoppingListRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new ShoppingListReadService(shoppingListRepository), currentUserAccessService);

    private static GetShoppingListByIdQueryHandler CreateShoppingListByIdHandler(
        IShoppingListReadModelRepository shoppingListRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new ShoppingListReadService(shoppingListRepository), currentUserAccessService);

    private static GetShoppingListsQueryHandler CreateShoppingListsHandler(
        IShoppingListReadModelRepository shoppingListRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new ShoppingListReadService(shoppingListRepository), currentUserAccessService);
}

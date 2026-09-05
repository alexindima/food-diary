using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;

public static class ShoppingListHttpMappings {
    extension(Guid userId) {
        public GetCurrentShoppingListQuery ToCurrentQuery() => new(userId);
        public GetShoppingListsQuery ToListQuery() => new(userId);
        public GetShoppingListByIdQuery ToGetByIdQuery(Guid userId1) =>
            new(userId1, userId);
        public DeleteShoppingListCommand ToDeleteCommand(Guid userId1) =>
            new(userId1, userId);
    }

    extension(CreateShoppingListHttpRequest request) {
        public CreateShoppingListCommand ToCommand(Guid userId) =>
                new(
                    userId,
                    request.Name,
                    request.Items?.Select(ToInput).ToList() ?? []);
    }

    extension(UpdateShoppingListHttpRequest request) {
        public UpdateShoppingListCommand ToCommand(
                Guid userId,
                Guid shoppingListId) =>
                new(
                    userId,
                    shoppingListId,
                    request.Name,
                    request.Items?.Select(ToInput).ToList());
    }

    private static ShoppingListItemInput ToInput(ShoppingListItemHttpRequest request) =>
        new(
            request.Id,
            request.ProductId,
            request.Name,
            request.Amount,
            request.Unit,
            request.Category,
            request.Aisle,
            request.Note,
            request.IsChecked,
            request.CheckedOnUtc,
            request.SortOrder);
}

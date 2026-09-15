using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Requests;

namespace FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Mappings;

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

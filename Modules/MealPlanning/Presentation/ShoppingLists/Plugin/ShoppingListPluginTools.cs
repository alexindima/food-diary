using System.ComponentModel;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Mappings;
using FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Requests;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Plugin;

[McpServerToolType]
public sealed class ShoppingListPluginTools(ISender sender, IHttpContextAccessor http, RecipePluginLinks links) {
    [McpServerTool(Name = RecipePluginAccess.ShoppingListTool, Title = "Create shopping list", ReadOnly = false, Destructive = false, OpenWorld = false)]
    [McpMeta("securitySchemes", JsonValue = "[{\"type\":\"oauth2\",\"scopes\":[\"" + RecipePluginAccess.ShoppingListScope + "\"]}]")]
    [Description("Create a separate FoodDiary shopping list. Leave out ingredients the user says they already have.")]
    public async Task<CallToolResult> CreateShoppingListAsync(
        string name, IReadOnlyList<PluginShoppingItem> items, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(name) || items is not { Count: > 0 } || items.Count > 100 ||
            items.Any(static item => item is null || string.IsNullOrWhiteSpace(item.Name))) {
            return Error("Provide a list name and shopping items with valid names.");
        }

        var webRequest = new CreateShoppingListHttpRequest(name, items.Select(static item => new ShoppingListItemHttpRequest(
            Id: null, ProductId: null, item.Name, Amount: null, Unit: null, Category: null, Aisle: null, Note: item.AmountText)).ToArray());
        Guid owner = RecipePluginAccess.GetOwner(http.HttpContext!.User);
        Result<ShoppingListModel> result = await sender.Send(webRequest.ToCommand(owner), cancellationToken);
        if (result.IsFailure) {
            return Error("FoodDiary could not save this list. Check the list and item details.");
        }

        ShoppingListModel list = result.Value;
        JsonElement saved = JsonSerializer.SerializeToElement(new {
            id = list.Id,
            name = list.Name,
            items = list.Items.Select(static item => new { name = item.Name, amountText = item.Note }),
            shoppingListsUrl = links.ShoppingLists.AbsoluteUri,
        });
        return new CallToolResult {
            Content = [new TextContentBlock { Text = saved.GetRawText() }],
            StructuredContent = saved,
        };
    }

    private static CallToolResult Error(string text) => new() {
        IsError = true,
        Content = [new TextContentBlock { Text = text }],
    };

    public sealed record PluginShoppingItem(string Name, string? AmountText = null);
}

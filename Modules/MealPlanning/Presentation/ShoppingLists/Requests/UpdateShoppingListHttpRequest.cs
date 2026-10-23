using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Requests;

public sealed record UpdateShoppingListHttpRequest(
    string? Name,
    IReadOnlyList<ShoppingListItemHttpRequest>? Items,
    [property: JsonRequired]
    [Required, RegularExpression("^[0-9a-f]{64}$")]
    string ExpectedRevision);

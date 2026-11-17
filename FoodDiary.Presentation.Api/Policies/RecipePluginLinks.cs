namespace FoodDiary.Presentation.Api.Policies;

public sealed record RecipePluginLinks(Uri WebOrigin) {
    public Uri Recipe(Guid id) => new(WebOrigin, $"/recipes/{id:D}/edit");
    public Uri ShoppingLists => new(WebOrigin, "/shopping-lists");
}

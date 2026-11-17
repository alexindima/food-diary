using System.Security.Claims;

namespace FoodDiary.Presentation.Api.Policies;

public static class RecipePluginAccess {
    public const string ResourcePath = "/api/mcp";
    public const string ResourceMetadataPath = "/.well-known/oauth-protected-resource/api/mcp";
    public const string AuthorizationPath = "/api/oauth/authorize";
    public const string TokenPath = "/api/oauth/token";
    public const string RevocationPath = "/api/oauth/revoke";
    public const string RecipeScope = "recipes:create";
    public const string ShoppingListScope = "shopping-lists:create";
    public const string RecipeTool = "save_private_recipe";
    public const string ShoppingListTool = "create_shopping_list";
    public const string SubjectClaim = "sub";

    public static Guid GetOwner(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(SubjectClaim), out Guid id) && id != Guid.Empty
            ? id : throw new InvalidOperationException("A delegated FoodDiary account is required.");
}

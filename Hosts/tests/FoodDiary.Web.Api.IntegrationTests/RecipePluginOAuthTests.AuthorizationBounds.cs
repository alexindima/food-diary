using System.Net;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class RecipePluginOAuthTests {
    [RequiresDockerTheory]
    [InlineData(true, "invalid_target")]
    [InlineData(false, "invalid_scope")]
    public async Task Consent_DeclinesRequestsWithoutAResourceOrAWriteScope(bool missingResource, string expectedError) {
        using HttpClient browser = CreateBrowser(WebOrigin, cookies: true);
        await RegisterBrowserAsync(browser);
        string scopes = missingResource ? RecipePluginAccess.RecipeScope : "offline_access";
        (Dictionary<string, string> parameters, _) = CreateAuthorization(scopes);
        if (missingResource) {
            parameters.Remove("resource");
        }

        using HttpResponseMessage response = await browser.GetAsync(AuthorizationUrl(parameters));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Uri redirect = Assert.IsType<Uri>(response.Headers.Location, exactMatch: false);
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(redirect.Query);
        Assert.Multiple(
            () => Assert.Equal(Callback, redirect.GetLeftPart(UriPartial.Path)),
            () => Assert.Equal(expectedError, query["error"].ToString()),
            () => Assert.Equal(parameters["state"], query["state"].ToString()),
            () => Assert.False(query.ContainsKey("code")));
    }
}

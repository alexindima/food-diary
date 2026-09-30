using System.Net;
using System.Net.Http.Json;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class PresentationBoundaryIntegrationTests {
    [RequiresDockerTheory]
    [InlineData("GET", "/api/v1/admin/meal-plans")]
    [InlineData("GET", "/api/v1/admin/meal-plans/recipes")]
    [InlineData("GET", "/api/v1/admin/meal-plans/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/api/v1/admin/meal-plans")]
    [InlineData("PUT", "/api/v1/admin/meal-plans/11111111-1111-1111-1111-111111111111")]
    public async Task MealPlanCatalog_RequiresAdminRole(string method, string path) {
        using HttpClient client = testAuthFactory.CreateClient();
        using var anonymousRequest = new HttpRequestMessage(new HttpMethod(method), path);
        using HttpResponseMessage anonymous = await client.SendAsync(anonymousRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        using var userRequest = new HttpRequestMessage(new HttpMethod(method), path);
        using HttpResponseMessage forbidden = await client.SendAsync(userRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [RequiresDockerFact]
    public async Task MealPlanCatalog_RejectsIncompletePublication() {
        using HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/admin/meal-plans", new {
            Name = "Incomplete",
            DietType = "Balanced",
            DurationDays = 1,
            IsPublished = true,
            Days = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

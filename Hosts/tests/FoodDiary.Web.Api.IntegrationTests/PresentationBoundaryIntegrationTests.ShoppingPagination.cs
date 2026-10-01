using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class PresentationBoundaryIntegrationTests {
    [RequiresDockerFact]
    public async Task ShoppingListPages_AreBoundedSearchableAndUserScoped() {
        using HttpClient client = apiFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAndGetAccessTokenAsync(client));
        for (int index = 0; index < 3; index++) {
            using HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/shopping-lists", new {
                name = "Pagination " + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                items = new[] { new { name = "Milk", isChecked = false }, new { name = "Bread", isChecked = true } },
            });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }
        using var overview = JsonDocument.Parse(await client.GetStringAsync("/api/v1/shopping-lists/overview"));
        Assert.Equal(3, overview.RootElement.GetProperty("lists").GetProperty("items").GetArrayLength());
        Assert.False(overview.RootElement.GetProperty("lists").GetProperty("hasMore").GetBoolean());
        Assert.Equal(2, overview.RootElement.GetProperty("selectedList").GetProperty("items").GetArrayLength());
        using var first = JsonDocument.Parse(await client.GetStringAsync("/api/v1/shopping-lists/page?limit=2"));
        using var second = JsonDocument.Parse(await client.GetStringAsync("/api/v1/shopping-lists/page?limit=2&page=2"));
        Assert.Equal(2, first.RootElement.GetArrayLength());
        Assert.Equal(1, second.RootElement.GetArrayLength());
        Assert.Equal(1, first.RootElement[0].GetProperty("remainingCount").GetInt32());
        Assert.DoesNotContain(second.RootElement[0].GetProperty("id").GetString(), first.RootElement.EnumerateArray().Select(row => row.GetProperty("id").GetString()), StringComparer.Ordinal);
        using var search = JsonDocument.Parse(await client.GetStringAsync("/api/v1/shopping-lists/page?search=pAGINATION%200"));
        Assert.Equal(1, search.RootElement.GetArrayLength());
        using HttpResponseMessage invalid = await client.GetAsync("/api/v1/shopping-lists/page?limit=1000");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using HttpClient other = apiFactory.CreateClient();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAndGetAccessTokenAsync(other));
        using var empty = JsonDocument.Parse(await other.GetStringAsync("/api/v1/shopping-lists/page"));
        Assert.Equal(0, empty.RootElement.GetArrayLength());
        using var emptyOverview = JsonDocument.Parse(await other.GetStringAsync("/api/v1/shopping-lists/overview"));
        Assert.Equal(JsonValueKind.Null, emptyOverview.RootElement.GetProperty("selectedList").ValueKind);
        Assert.Equal(0, emptyOverview.RootElement.GetProperty("lists").GetProperty("items").GetArrayLength());
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Requests;
using FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Responses;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class ShoppingListRevisionApiTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [RequiresDockerFact]
    public async Task SequentialStaleSnapshotReturnsConflictWithoutRemovingCommittedItemsAsync() {
        using HttpClient firstClient = factory.CreateClient();
        using HttpClient secondClient = factory.CreateClient();
        string token = await RegisterAsync(firstClient);
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        ShoppingListHttpResponse created = await CreateListAsync(firstClient);
        string route = $"/api/v1/shopping-lists/{created.Id}";
        ShoppingListHttpResponse firstSnapshot = await GetListAsync(firstClient, route);
        ShoppingListHttpResponse staleSnapshot = await GetListAsync(secondClient, route);
        Assert.Equal(created.Revision, firstSnapshot.Revision);
        Assert.Equal(firstSnapshot.Revision, staleSnapshot.Revision);
        var firstRequest = new UpdateShoppingListHttpRequest(Name: null,
            [.. firstSnapshot.Items.Select(ToRequest), NewItem("Added by A", 1)], firstSnapshot.Revision);
        var staleRequest = new UpdateShoppingListHttpRequest("Must not rename",
            [.. staleSnapshot.Items.Select(ToRequest), NewItem("Added by B", 1)], staleSnapshot.Revision);

        using HttpResponseMessage firstWrite = await firstClient.PatchAsJsonAsync(route, firstRequest);
        Assert.Equal(HttpStatusCode.OK, firstWrite.StatusCode);
        ShoppingListHttpResponse? firstResponse = await firstWrite.Content.ReadFromJsonAsync<ShoppingListHttpResponse>(JsonOptions);
        Assert.NotNull(firstResponse);
        ShoppingListHttpResponse committed = await GetListAsync(firstClient, route);
        Assert.Equal(firstResponse.Revision, committed.Revision);
        Assert.NotEqual(firstSnapshot.Revision, committed.Revision, StringComparer.Ordinal);
        Assert.Contains(committed.Items, item => string.Equals(item.Name, "Added by A", StringComparison.Ordinal));

        // A's response and authoritative GET have both completed before B writes
        // its older body. A database-only concurrent transaction check is insufficient.
        using HttpResponseMessage staleWrite = await secondClient.PatchAsJsonAsync(route, staleRequest);
        Assert.Equal(HttpStatusCode.Conflict, staleWrite.StatusCode);
        ErrorPayload? error = await staleWrite.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        Assert.NotNull(error);
        Assert.Equal("ShoppingList.RevisionConflict", error.Error);
        ShoppingListHttpResponse afterConflict = await GetListAsync(firstClient, route);
        Assert.Equal(committed.Revision, afterConflict.Revision);
        Assert.Equal(created.Name, afterConflict.Name);
        Assert.Equal(2, afterConflict.Items.Count);
        Assert.Contains(afterConflict.Items, item => string.Equals(item.Name, "Added by A", StringComparison.Ordinal));
        Assert.DoesNotContain(afterConflict.Items, item => string.Equals(item.Name, "Added by B", StringComparison.Ordinal));

        // A newly read baseline is accepted for a legitimate subsequent edit.
        using HttpResponseMessage checkedWrite = await firstClient.PatchAsJsonAsync(route,
            new UpdateShoppingListHttpRequest(Name: null,
                [.. afterConflict.Items.Select(item => ToRequest(item) with {
                    IsChecked = true,
                    CheckedOnUtc = new DateTime(1999, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999),
                })], afterConflict.Revision));
        Assert.Equal(HttpStatusCode.OK, checkedWrite.StatusCode);
        ShoppingListHttpResponse? checkedResponse = await checkedWrite.Content.ReadFromJsonAsync<ShoppingListHttpResponse>(JsonOptions);
        Assert.NotNull(checkedResponse);
        ShoppingListHttpResponse checkedPersisted = await GetListAsync(firstClient, route);
        Assert.Equal(checkedResponse.Revision, checkedPersisted.Revision);
        Assert.All(checkedPersisted.Items, item => Assert.True(item.IsChecked));
    }

    [RequiresDockerTheory]
    [InlineData(null, true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("not-a-revision", false)]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", false)]
    public async Task MissingOrMalformedRevisionReturnsBadRequestAndPreservesListAsync(string? revision, bool omitRevision) {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(client));
        ShoppingListHttpResponse created = await CreateListAsync(client);
        string route = $"/api/v1/shopping-lists/{created.Id}";
        object payload = omitRevision
            ? new { name = "Must not apply", items = Array.Empty<object>() }
            : new { name = "Must not apply", items = Array.Empty<object>(), expectedRevision = revision };

        using HttpResponseMessage response = await client.PatchAsJsonAsync(route, payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ShoppingListHttpResponse persisted = await GetListAsync(client, route);
        Assert.Equal(created.Revision, persisted.Revision);
        Assert.Equal(created.Name, persisted.Name);
        Assert.Single(persisted.Items);
    }

    [RequiresDockerFact]
    public async Task ForeignOwnerCannotUseAnotherListsRevisionAsync() {
        using HttpClient owner = factory.CreateClient();
        using HttpClient stranger = factory.CreateClient();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(owner));
        stranger.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(stranger));
        ShoppingListHttpResponse created = await CreateListAsync(owner);
        string route = $"/api/v1/shopping-lists/{created.Id}";

        using HttpResponseMessage response = await stranger.PatchAsJsonAsync(route,
            new UpdateShoppingListHttpRequest("Must not apply", [], created.Revision));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(created.Revision, (await GetListAsync(owner, route)).Revision);
    }

    [RequiresDockerFact]
    public async Task RenameAndClearKeepTheirExistingOptionalFieldOmissionContractAsync() {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(client));
        ShoppingListHttpResponse created = await CreateListAsync(client);
        string route = $"/api/v1/shopping-lists/{created.Id}";
        using HttpResponseMessage rename = await client.PatchAsJsonAsync(route,
            new { name = "Renamed", expectedRevision = created.Revision });
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        ShoppingListHttpResponse renamed = await GetListAsync(client, route);
        Assert.Equal("Renamed", renamed.Name);
        Assert.Single(renamed.Items);

        using HttpResponseMessage clear = await client.PatchAsJsonAsync(route,
            new { items = Array.Empty<object>(), expectedRevision = renamed.Revision });
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        ShoppingListHttpResponse cleared = await GetListAsync(client, route);
        Assert.Equal("Renamed", cleared.Name);
        Assert.Empty(cleared.Items);
    }

    [RequiresDockerFact]
    public async Task CreateCurrentDetailAndOverviewExposeTheSameCommittedRevisionAsync() {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAsync(client));
        ShoppingListHttpResponse created = await CreateListAsync(client);
        Assert.Matches("^[0-9a-f]{64}$", created.Revision);
        Assert.Equal(created.Revision, (await GetListAsync(client, $"/api/v1/shopping-lists/{created.Id}")).Revision);
        Assert.Equal(created.Revision, (await GetListAsync(client, "/api/v1/shopping-lists/current")).Revision);
        using HttpResponseMessage overviewResponse = await client.GetAsync("/api/v1/shopping-lists/overview");
        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        ShoppingListOverviewHttpResponse? overview = await overviewResponse.Content.ReadFromJsonAsync<ShoppingListOverviewHttpResponse>(JsonOptions);
        Assert.NotNull(overview);
        Assert.NotNull(overview.SelectedList);
        Assert.Equal(created.Revision, overview.SelectedList.Revision);
    }

    private static async Task<string> RegisterAsync(HttpClient client) {
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"shopping-revision-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        AuthPayload? payload = await registration.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        return payload.AccessToken;
    }

    private static async Task<ShoppingListHttpResponse> CreateListAsync(HttpClient client) {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/shopping-lists",
            new CreateShoppingListHttpRequest("Revision test list", [NewItem("Original", 0)]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        ShoppingListHttpResponse? list = await response.Content.ReadFromJsonAsync<ShoppingListHttpResponse>(JsonOptions);
        Assert.NotNull(list);
        return list;
    }

    private static async Task<ShoppingListHttpResponse> GetListAsync(HttpClient client, string route) {
        using HttpResponseMessage response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ShoppingListHttpResponse? list = await response.Content.ReadFromJsonAsync<ShoppingListHttpResponse>(JsonOptions);
        Assert.NotNull(list);
        return list;
    }

    private static ShoppingListItemHttpRequest NewItem(string name, int order) =>
        new(Id: null, ProductId: null, name, 1, "Pcs", Category: null, Aisle: null, Note: null,
            IsChecked: false, CheckedOnUtc: null, order);

    private static ShoppingListItemHttpRequest ToRequest(ShoppingListItemHttpResponse item) =>
        new(item.Id, item.ProductId, item.Name, item.Amount, item.Unit, item.Category,
            item.Aisle, item.Note, item.IsChecked, item.CheckedOnUtc, item.SortOrder);

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken);

    [ExcludeFromCodeCoverage]
    private sealed record ErrorPayload(string Error);
}

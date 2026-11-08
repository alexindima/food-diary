using System.Net;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class UserActivityApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task ForegroundActivity_RequiresAuthenticationAndUsesServerTimeWithoutChangingLogin() {
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpResponseMessage anonymous = await browser.PostAsync("/api/v1/users/activity", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, browser);
        using HttpResponseMessage background = await browser.GetAsync("/api/v1/users/info");
        using HttpResponseMessage refresh = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, background.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Null(await GetActivityAsync(identity.Id));
        DateTime? loginBefore = await GetLoginAsync(identity.Id);
        DateTime before = DateTime.UtcNow;
        using HttpResponseMessage activity = await browser.PostAsync("/api/v1/users/activity", content: null);
        Assert.Equal(HttpStatusCode.NoContent, activity.StatusCode);
        DateTime first = Assert.IsType<DateTime>(await GetActivityAsync(identity.Id));
        Assert.InRange(first, before, DateTime.UtcNow);
        using HttpResponseMessage repeated = await browser.PostAsync("/api/v1/users/activity", content: null);
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        Assert.Equal(first, await GetActivityAsync(identity.Id));
        Assert.Equal(loginBefore, await GetLoginAsync(identity.Id));
    }

    [RequiresDockerFact]
    public async Task AdminAccounts_ProjectAndFilterActivityIncludingUnknownUsers() {
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, browser);
        using HttpResponseMessage unknown = await administrator.GetAsync($"/api/v1/admin/users/{identity.Id}");
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        JsonElement unknownUser = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, unknownUser.GetProperty("lastActiveAtUtc").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, unknownUser.GetProperty("lastLoginAtUtc").ValueKind);
        using HttpResponseMessage activity = await browser.PostAsync("/api/v1/users/activity", content: null);
        Assert.Equal(HttpStatusCode.NoContent, activity.StatusCode);
        DateTime recorded = Assert.IsType<DateTime>(await GetActivityAsync(identity.Id));
        string date = recorded.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using HttpResponseMessage filtered = await administrator.GetAsync($"/api/v1/admin/users?lastActiveFrom={date}&lastActiveTo={date}&search={identity.Email}");
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
        JsonElement page = await filtered.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement item = Assert.Single(page.GetProperty("data").EnumerateArray());
        Assert.Equal(identity.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(recorded, item.GetProperty("lastActiveAtUtc").GetDateTime());
        using HttpResponseMessage reversed = await administrator.GetAsync("/api/v1/admin/users?lastActiveFrom=2030-01-03&lastActiveTo=2030-01-02");
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
    }

    private async Task<DateTime?> GetActivityAsync(Guid id) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        UsersDbContext context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        return await context.UserActivities.AsNoTracking().Where(activity => activity.UserId == new UserId(id))
            .Select(activity => (DateTime?)activity.LastActiveAtUtc).SingleOrDefaultAsync();
    }

    private async Task<DateTime?> GetLoginAsync(Guid id) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<UsersDbContext>().Users.AsNoTracking()
            .Where(user => user.Id == new UserId(id)).Select(user => user.LastLoginAtUtc).SingleAsync();
    }
}

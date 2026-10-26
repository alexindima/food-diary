using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalCalendarValidationIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task LocalMondayNextWeek_PassesRealValidationAndPersistsCalendarLabelAsync() {
        // Authentication setup uses the normal clock; only the calendar scenario runs in the past.
        using HttpClient registrationClient = factory.CreateClient();
        string email = $"weekly-calendar-{Guid.NewGuid():N}@example.test";
        using HttpResponseMessage registration = await registrationClient.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        using var auth = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());

        var localMonday = DateOnly.FromDateTime(DateTime.UtcNow);
        while (localMonday.DayOfWeek != DayOfWeek.Monday) {
            localMonday = localMonday.AddDays(-1);
        }
        var mondayUtc = localMonday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var clock = new FixedTimeProvider(mondayUtc.AddHours(-3.5));
        await using WebApplicationFactory<Program> clocked = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock))));
        using HttpClient client = clocked.CreateClient();
        string browserSession = AuthenticationResponseCookies.ReadBrowserSession(registration);
        AuthenticationResponseCookies.ApplyBrowserSession(client, browserSession);
        DateOnly nextWeek = localMonday.AddDays(7);
        string weekLabel = nextWeek.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/weekly-goals", new {
            weekStart = weekLabel,
            targetDays = 5,
            reminderEnabled = false,
            timeZoneId = "Asia/Tbilisi",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(weekLabel, result.RootElement.GetProperty("weekStart").GetString());
        Assert.Equal(5, result.RootElement.GetProperty("targetDays").GetInt32());
        HttpResponseMessage read = await client.GetAsync($"/api/v1/weekly-goals?weekStart={weekLabel}&timeZoneId=Asia%2FTbilisi");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var persisted = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        Assert.Equal(result.RootElement.GetProperty("id").GetGuid(), persisted.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(weekLabel, persisted.RootElement.GetProperty("weekStart").GetString());
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}

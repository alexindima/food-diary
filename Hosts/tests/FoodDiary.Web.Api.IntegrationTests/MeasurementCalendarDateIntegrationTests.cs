using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MeasurementCalendarDateIntegrationTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData("Asia/Tbilisi", "2026-03-01")]
    [InlineData("America/Los_Angeles", "2026-03-08")]
    [InlineData("Europe/Berlin", "2026-03-29")]
    [InlineData("Asia/Kathmandu", "2024-02-29")]
    [InlineData("Pacific/Kiritimati", "2026-01-01")]
    [InlineData("Pacific/Pago_Pago", "2026-01-01")]
    public async Task Summary_ReturnsOnlySelectedCalendarDay_FromPersistedWeightAndWaist(string zoneId, string dateText) {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"calendar-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        using var auth = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.RootElement.GetProperty("accessToken").GetString());
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        for (int offset = -1; offset <= 1; offset++) {
            var date = day.AddDays(offset).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            using HttpResponseMessage weight = await client.PostAsJsonAsync("/api/v1/weight-entries", new { Date = date, WeightKg = 75 + (offset * 10) });
            using var waistRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/waist-entries") {
                Content = JsonContent.Create(new { Date = date, CircumferenceCm = 82 + (offset * 10) }),
            };
            waistRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            using HttpResponseMessage waist = await client.SendAsync(waistRequest);
            Assert.True(weight.IsSuccessStatusCode, await weight.Content.ReadAsStringAsync());
            Assert.True(waist.IsSuccessStatusCode, await waist.Content.ReadAsStringAsync());
        }
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        string from = Uri.EscapeDataString(TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone).ToString("O", CultureInfo.InvariantCulture));
        string to = Uri.EscapeDataString(TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone).AddTicks(-1).ToString("O", CultureInfo.InvariantCulture));
        using HttpResponseMessage response = await client.GetAsync($"/api/v1/statistics/summary?dateFrom={from}&dateTo={to}&bodyDateFrom={dateText}&bodyDateTo={dateText}&quantizationDays=1");
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement weightPoint = Assert.Single(result.RootElement.GetProperty("weight").EnumerateArray());
        JsonElement waistPoint = Assert.Single(result.RootElement.GetProperty("waist").EnumerateArray());
        Assert.Multiple(
            () => Assert.Equal(75, weightPoint.GetProperty("averageWeightKg").GetDouble()),
            () => Assert.Equal(82, waistPoint.GetProperty("averageCircumferenceCm").GetDouble()),
            () => Assert.Equal(dateText, weightPoint.GetProperty("startDate").GetString()![..10]),
            () => Assert.Equal(dateText, waistPoint.GetProperty("startDate").GetString()![..10]));
    }
}

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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
        var localMonday = DateOnly.FromDateTime(DateTime.UtcNow);
        while (localMonday.DayOfWeek != DayOfWeek.Monday) {
            localMonday = localMonday.AddDays(-1);
        }
        var mondayUtc = localMonday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var clock = new FixedTimeProvider(mondayUtc.AddHours(-3.5));
        await using WebApplicationFactory<Program> clocked = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock))));
        using HttpClient client = clocked.CreateClient();
        string email = $"weekly-calendar-{Guid.NewGuid():N}@example.test";
        HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        using var auth = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        var userId = new UserId(auth.RootElement.GetProperty("user").GetProperty("id").GetGuid());
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.RootElement.GetProperty("accessToken").GetString());
        var sessionId = new RefreshTokenSessionId(Guid.Parse(jwt.Claims.Single(claim => string.Equals(claim.Type, "refresh_session_id", StringComparison.Ordinal)).Value));
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        FoodDiaryDbContext database = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        long securityVersion = database.Users.Where(user => user.Id == userId).Select(user => user.SecurityVersion).Single();
        IJwtTokenGenerator tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        // Mint through the normal host clock; the calendar host intentionally runs in the past.
        string accessToken = tokens.GenerateAccessToken(userId, email, [], DateTime.UtcNow.AddHours(1), securityVersion, sessionId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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

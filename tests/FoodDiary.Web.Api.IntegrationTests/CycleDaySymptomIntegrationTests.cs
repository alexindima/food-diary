using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Domain.Enums;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CycleDaySymptomIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task UpsertDay_WithClearedSymptomCategory_PreservesOtherDayObservations() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Guid cycleProfileId = await CreateCycleAsync(client);
        DateTime date = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        HttpResponseMessage initialResponse = await client.PutAsJsonAsync(
            $"/api/v1/cycles/{cycleProfileId}/days",
            new UpsertCycleDayHttpRequest(
                date,
                new BleedingLogHttpModel(
                    (int)BleedingType.Bleeding,
                    (int)CycleFlowLevel.Medium,
                    PainImpact: 3,
                    Notes: null,
                    ClearNotes: false),
                [
                    new SymptomLogHttpModel((int)CycleSymptomCategory.Pain, 4, [], Note: null, ClearNote: false),
                    new SymptomLogHttpModel((int)CycleSymptomCategory.Mood, 6, [], Note: null, ClearNote: false),
                ],
                new FertilitySignalHttpModel(
                    BasalBodyTemperatureCelsius: 36.6,
                    OvulationTestResult: null,
                    CervicalFluid: null,
                    HadSex: null,
                    Notes: null,
                    ClearNotes: false)));
        initialResponse.EnsureSuccessStatusCode();

        HttpResponseMessage clearResponse = await client.PutAsJsonAsync(
            $"/api/v1/cycles/{cycleProfileId}/days",
            new UpsertCycleDayHttpRequest(
                date,
                Bleeding: null,
                Symptoms: [],
                FertilitySignal: null,
                ClearSymptomCategories: [(int)CycleSymptomCategory.Pain]));
        clearResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await clearResponse.Content.ReadAsStringAsync());

        JsonElement root = json.RootElement;
        JsonElement bleeding = Assert.Single(root.GetProperty("bleedingEntries").EnumerateArray());
        JsonElement symptom = Assert.Single(root.GetProperty("symptoms").EnumerateArray());
        Assert.Equal((int)BleedingType.Bleeding, bleeding.GetProperty("type").GetInt32());
        Assert.Equal((int)CycleSymptomCategory.Mood, symptom.GetProperty("category").GetInt32());
        Assert.Equal(36.6, root.GetProperty("fertilitySignal").GetProperty("basalBodyTemperatureCelsius").GetDouble());
    }

    [Fact]
    public async Task UpsertDay_WithClearedFertilitySignal_PreservesOtherDayObservations() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Guid cycleProfileId = await CreateCycleAsync(client);
        DateTime date = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);

        HttpResponseMessage initialResponse = await client.PutAsJsonAsync(
            $"/api/v1/cycles/{cycleProfileId}/days",
            new UpsertCycleDayHttpRequest(
                date,
                new BleedingLogHttpModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 3, Notes: null, ClearNotes: false),
                [new SymptomLogHttpModel((int)CycleSymptomCategory.Pain, 4, [], Note: null, ClearNote: false)],
                new FertilitySignalHttpModel(36.6, (int)OvulationTestResult.Negative, CervicalFluid: null, HadSex: null, Notes: null, ClearNotes: false)));
        initialResponse.EnsureSuccessStatusCode();

        HttpResponseMessage clearResponse = await client.PutAsJsonAsync(
            $"/api/v1/cycles/{cycleProfileId}/days",
            new UpsertCycleDayHttpRequest(
                date,
                Bleeding: null,
                Symptoms: [],
                FertilitySignal: null,
                ClearFertilitySignal: true));
        clearResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await clearResponse.Content.ReadAsStringAsync());

        JsonElement root = json.RootElement;
        Assert.Single(root.GetProperty("bleedingEntries").EnumerateArray());
        Assert.Single(root.GetProperty("symptoms").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("fertilitySignal").ValueKind);
    }

    private static async Task<Guid> CreateCycleAsync(HttpClient client) {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/cycles",
            new CreateCycleHttpRequest(
                new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                (int)CycleTrackingMode.PeriodTracking,
                AverageCycleLength: 28,
                AveragePeriodLength: 5,
                LutealLength: 14,
                IsRegular: true,
                IsOnboardingComplete: true,
                ShowFertilityEstimates: false,
                DiscreetNotifications: true,
                Notes: null));
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest($"cycle-symptom-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }
}

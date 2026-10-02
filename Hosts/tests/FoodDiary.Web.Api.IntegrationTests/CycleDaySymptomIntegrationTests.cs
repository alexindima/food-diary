using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Cycles.Presentation.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CycleDaySymptomIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task UpsertFactor_WithId_ChangesDateWithoutDuplicatingAndRejectsCollision() {
        HttpClient client = factory.CreateClient();
        string token = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Guid profileId = await CreateCycleAsync(client);
        var request = new UpsertCycleFactorHttpRequest((int)CycleFactorType.HormonalContraception,
            new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc), EndDate: null, Notes: "first", ClearNotes: false);
        HttpResponseMessage created = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/factors", request);
        created.EnsureSuccessStatusCode();
        using var initial = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Guid factorId = initial.RootElement.GetProperty("factors")[0].GetProperty("id").GetGuid();
        UpsertCycleFactorHttpRequest editedRequest = request with { FactorId = factorId, StartDate = request.StartDate.AddDays(1) };
        HttpResponseMessage edited = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/factors", editedRequest);
        edited.EnsureSuccessStatusCode();
        using var updated = JsonDocument.Parse(await edited.Content.ReadAsStringAsync());
        JsonElement factors = updated.RootElement.GetProperty("factors");
        Assert.Equal(1, factors.GetArrayLength());
        Assert.Equal(factorId, factors[0].GetProperty("id").GetGuid());
        Assert.Equal(editedRequest.StartDate.Date, factors[0].GetProperty("startDate").GetDateTime().Date);
        HttpResponseMessage second = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/factors", request);
        second.EnsureSuccessStatusCode();
        HttpResponseMessage conflict = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/factors", request with { FactorId = factorId });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var error = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync());
        Assert.Equal("Cycle.FactorIdentityConflict", error.RootElement.GetProperty("error").GetString());
        HttpResponseMessage current = await client.GetAsync("/api/v1/cycles/current");
        current.EnsureSuccessStatusCode();
        using var persisted = JsonDocument.Parse(await current.Content.ReadAsStringAsync());
        Assert.Equal(2, persisted.RootElement.GetProperty("factors").GetArrayLength());
    }

    [RequiresDockerFact]
    public async Task CreateCycle_WithOversizedNotes_ReturnsValidationErrorAndAcceptsTrimmedLimit() {
        HttpClient client = factory.CreateClient();
        string token = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new CreateCycleHttpRequest(
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 28, AveragePeriodLength: 5, LutealLength: 14,
            IsRegular: true, IsOnboardingComplete: true, ShowFertilityEstimates: false,
            DiscreetNotifications: true, Notes: new string('x', 1025), CycleTrackingConsentGranted: true);

        HttpResponseMessage invalid = await client.PostAsJsonAsync("/api/v1/cycles", request);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var error = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        Assert.Equal("Validation.Invalid", error.RootElement.GetProperty("error").GetString());

        HttpResponseMessage valid = await client.PostAsJsonAsync("/api/v1/cycles",
            request with { Notes = "  " + new string('x', 1024) + "  " });
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        using var body = JsonDocument.Parse(await valid.Content.ReadAsStringAsync());
        Assert.Equal(new string('x', 1024), body.RootElement.GetProperty("notes").GetString());
    }

    [RequiresDockerFact]
    public async Task DeleteCycle_WithOwnedProfile_ReturnsNoContentAndClearsCurrentCycle() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Guid cycleProfileId = await CreateCycleAsync(client);

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/cycles/{cycleProfileId}");
        HttpResponseMessage repeatedDeleteResponse = await client.DeleteAsync($"/api/v1/cycles/{cycleProfileId}");
        HttpResponseMessage currentResponse = await client.GetAsync("/api/v1/cycles/current");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, repeatedDeleteResponse.StatusCode);
        currentResponse.EnsureSuccessStatusCode();
        Assert.Empty(await currentResponse.Content.ReadAsStringAsync());
    }

    [RequiresDockerFact]
    public async Task DeleteCycle_WithoutAuthentication_ReturnsUnauthorized() {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/cycles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresDockerFact]
    public async Task DeleteCycle_WithForeignProfile_ReturnsNotFoundAndPreservesOwnerCycle() {
        HttpClient ownerClient = factory.CreateClient();
        string ownerToken = await RegisterAndGetAccessTokenAsync(ownerClient);
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        Guid cycleProfileId = await CreateCycleAsync(ownerClient);
        HttpClient otherClient = factory.CreateClient();
        string otherToken = await RegisterAndGetAccessTokenAsync(otherClient);
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        HttpResponseMessage deleteResponse = await otherClient.DeleteAsync($"/api/v1/cycles/{cycleProfileId}");
        HttpResponseMessage currentResponse = await ownerClient.GetAsync("/api/v1/cycles/current");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        currentResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await currentResponse.Content.ReadAsStringAsync());
        Assert.Equal(cycleProfileId, json.RootElement.GetProperty("id").GetGuid());
    }

    [RequiresDockerFact]
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

    [RequiresDockerFact]
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

    [RequiresDockerFact]
    public async Task UpsertFertility_RejectsInvalidValuesAndPersistsBoundaries() {
        HttpClient client = factory.CreateClient();
        string token = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Guid profileId = await CreateCycleAsync(client);
        var signal = new FertilitySignalHttpModel(BasalBodyTemperatureCelsius: 36.62,
            OvulationTestResult: null, CervicalFluid: "saved", HadSex: false, Notes: null, ClearNotes: false);
        var request = new UpsertCycleDayHttpRequest(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            Bleeding: null, Symptoms: [], FertilitySignal: signal);
        foreach ((string scenario, FertilitySignalHttpModel invalidSignal) in new[] {
            ("temperature-below-range", signal with { BasalBodyTemperatureCelsius = 33.99 }),
            ("temperature-above-range", signal with { BasalBodyTemperatureCelsius = 42.01 }),
            ("cervical-fluid-too-long", signal with { CervicalFluid = new string('x', 129) }),
        }) {
            HttpResponseMessage invalid = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/days",
                request with { FertilitySignal = invalidSignal });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var error = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
            Assert.Equal("Validation.Invalid", error.RootElement.GetProperty("error").GetString());
            using var snapshots = JsonDocument.Parse(await File.ReadAllTextAsync(
                SnapshotPathResolver.GetPath("cycle-fertility-validation-contract.json")));
            var actual = new {
                Status = (int)invalid.StatusCode,
                Error = error.RootElement.GetProperty("error").GetString(),
                Fields = error.RootElement.GetProperty("errors").EnumerateObject()
                    .Select(static field => field.Name).Order(StringComparer.Ordinal).ToArray(),
            };
            Assert.Equal(JsonSerializer.Serialize(snapshots.RootElement.GetProperty(scenario)),
                JsonSerializer.Serialize(actual));
        }
        foreach (double temperature in new[] { 34.0, 42.0 }) {
            HttpResponseMessage valid = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/days",
                request with {
                    FertilitySignal = signal with {
                        BasalBodyTemperatureCelsius = temperature,
                        CervicalFluid = "  " + new string('x', 128) + "  ",
                    },
                });
            Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        }
        HttpResponseMessage current = await client.GetAsync("/api/v1/cycles/current");
        current.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await current.Content.ReadAsStringAsync());
        JsonElement stored = Assert.Single(body.RootElement.GetProperty("fertilitySignals").EnumerateArray());
        Assert.Multiple(
            () => Assert.Equal(42, stored.GetProperty("basalBodyTemperatureCelsius").GetDouble()),
            () => Assert.Equal(new string('x', 128), stored.GetProperty("cervicalFluid").GetString()));
    }

    [RequiresDockerFact]
    public async Task UpdateEpisode_RejectsEndBeforeStartAndAcceptsDateBoundaries() {
        HttpClient client = factory.CreateClient();
        string token = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Guid profileId = await CreateCycleAsync(client);
        var start = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        HttpResponseMessage confirmed = await client.PutAsJsonAsync($"/api/v1/cycles/{profileId}/period-start", new { Date = start });
        confirmed.EnsureSuccessStatusCode();
        using var initial = JsonDocument.Parse(await confirmed.Content.ReadAsStringAsync());
        Guid episodeId = Assert.Single(initial.RootElement.GetProperty("menstrualEpisodes").EnumerateArray()).GetProperty("id").GetGuid();
        string url = $"/api/v1/cycles/{profileId}/menstrual-episodes/{episodeId}";
        HttpResponseMessage invalid = await client.PutAsJsonAsync(url, new UpdateMenstrualEpisodeHttpRequest(start, start.AddDays(-1)));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var error = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("cycle-episode-date-validation-contract.json")));
        var actual = new {
            Status = (int)invalid.StatusCode,
            Error = error.RootElement.GetProperty("error").GetString(),
            Fields = error.RootElement.GetProperty("errors").EnumerateObject().Select(static field => field.Name)
                .Order(StringComparer.Ordinal).ToArray(),
        };
        Assert.Equal(JsonSerializer.Serialize(snapshot.RootElement), JsonSerializer.Serialize(actual));
        HttpResponseMessage current = await client.GetAsync("/api/v1/cycles/current");
        current.EnsureSuccessStatusCode();
        using var persisted = JsonDocument.Parse(await current.Content.ReadAsStringAsync());
        JsonElement episode = Assert.Single(persisted.RootElement.GetProperty("menstrualEpisodes").EnumerateArray());
        Assert.Equal(start, episode.GetProperty("startDate").GetDateTime());
        foreach (DateTime? end in new DateTime?[] { start, start.AddDays(1), null }) {
            HttpResponseMessage valid = await client.PutAsJsonAsync(url, new UpdateMenstrualEpisodeHttpRequest(start, end));
            Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        }
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
                Notes: null,
                CycleTrackingConsentGranted: true,
                FertilitySignalsConsentGranted: true));
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

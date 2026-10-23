using HttpResults = Microsoft.AspNetCore.Http.Results;
using System.Net;
using System.Text.Json;
using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DevelopmentRuntimeHeadersContractTests {
    [Fact]
    public async Task Headers_RespectTheLocalDevelopmentHttpContract() {
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(SnapshotPathResolver.GetPath("development-runtime-headers-contract.json")));
        JsonElement contract = snapshot.RootElement;
        string buildHeader = contract.GetProperty("fingerprintHeader").GetString()!;
        string instanceHeader = contract.GetProperty("instanceHeader").GetString()!;
        foreach (JsonElement example in contract.GetProperty("cases").EnumerateArray()) {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions {
                EnvironmentName = example.GetProperty("environment").GetString(),
            });
            builder.WebHost.UseTestServer();
            await using WebApplication app = builder.Build();
            string? address = example.GetProperty("address").GetString();
            app.Use((context, next) => {
                context.Connection.RemoteIpAddress = address is null ? null : IPAddress.Parse(address);
                return next(context);
            });
            app.UseMiddleware<DevelopmentRuntimeHeadersMiddleware>();
            app.Use((context, next) => {
                context.Response.Headers[buildHeader] = "stale-downstream-build";
                context.Response.Headers[instanceHeader] = "stale-downstream-instance";
                return next(context);
            });
            app.MapGet("/api/probe", () => HttpResults.NoContent());
            app.MapGet("/health/probe", () => HttpResults.NoContent());
            await app.StartAsync();
            using HttpClient client = app.GetTestClient();
            using HttpResponseMessage response = await client.GetAsync(example.GetProperty("path").GetString());
            bool expected = example.GetProperty("diagnostics").GetBoolean();
            Assert.Multiple(
                () => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode),
                () => Assert.Equal(expected, response.Headers.Contains(buildHeader)),
                () => Assert.Equal(expected, response.Headers.Contains(instanceHeader)));
            if (expected) {
                Assert.Matches(contract.GetProperty("fingerprintPattern").GetString()!, Assert.Single(response.Headers.GetValues(buildHeader)));
                Assert.True(Guid.TryParseExact(Assert.Single(response.Headers.GetValues(instanceHeader)), "D", out _));
            }
        }
    }
}

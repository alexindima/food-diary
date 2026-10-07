using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class OpenApiSdkExportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Fact]
    public async Task ExportOpenApiAsync() {
        await using WebApplicationFactory<Program> sdkFactory = CreateSdkFactory();
        using HttpClient client = sdkFactory.CreateClient();
        string content = await client.GetStringAsync("/swagger/v1/swagger.json", CancellationToken.None);
        using var document = JsonDocument.Parse(content);
        Assert.True(document.RootElement.TryGetProperty("openapi", out _));
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/v{version}/products", out _));

        string? exportPath = Environment.GetEnvironmentVariable("FOODDIARY_SDK_OPENAPI_PATH");
        if (!string.IsNullOrWhiteSpace(exportPath)) {
            string absolutePath = Path.GetFullPath(exportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllTextAsync(absolutePath, content, CancellationToken.None);
        }
    }

    [Fact]
    public async Task PreserveNullableReferenceContractsAsync() {
        await using WebApplicationFactory<Program> sdkFactory = CreateSdkFactory();
        using HttpClient client = sdkFactory.CreateClient();
        string content = await client.GetStringAsync("/swagger/v1/swagger.json", CancellationToken.None);
        using var document = JsonDocument.Parse(content);
        JsonElement schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        JsonElement bleeding = schemas.GetProperty("UpsertCycleDayHttpRequest").GetProperty("properties").GetProperty("bleeding");
        Assert.True(bleeding.GetProperty("nullable").GetBoolean());
        Assert.Equal("#/components/schemas/BleedingLogHttpModel", bleeding.GetProperty("allOf")[0].GetProperty("$ref").GetString());

        JsonElement vision = schemas.GetProperty("FoodRecognitionJobHttpResponse").GetProperty("properties").GetProperty("vision");
        Assert.True(vision.GetProperty("nullable").GetBoolean());
        JsonElement name = schemas.GetProperty("MealPlanHttpResponse").GetProperty("properties").GetProperty("name");
        Assert.False(name.TryGetProperty("nullable", out JsonElement nullableName) && nullableName.GetBoolean());
    }

    [Fact]
    public async Task VerifyOpenApiContractSnapshotsAsync() {
        using HttpClient client = factory.CreateClient();
        string content = await client.GetStringAsync("/swagger/v1/swagger.json", CancellationToken.None);
        using var document = JsonDocument.Parse(content);
        JsonElement root = document.RootElement;
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-full-contract.json",
            PresentationBoundaryIntegrationTests.BuildFullOpenApiSnapshot(root));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-focused-contract.json",
            PresentationBoundaryIntegrationTests.BuildFocusedOpenApiSnapshot(root));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-auth-admin-contract.json",
            PresentationBoundaryIntegrationTests.BuildAuthAdminOpenApiSnapshot(root));
    }

    [Fact]
    public async Task IncludeInternalTelemetryOnlyInSdkViewAsync() {
        await using WebApplicationFactory<Program> sdkFactory = CreateSdkFactory();
        using HttpClient sdkClient = sdkFactory.CreateClient();
        using var sdk = JsonDocument.Parse(await sdkClient.GetStringAsync("/swagger/v1/swagger.json", CancellationToken.None));
        using HttpClient publicClient = factory.CreateClient();
        using var published = JsonDocument.Parse(await publicClient.GetStringAsync("/swagger/v1/swagger.json", CancellationToken.None));
        foreach (string path in new[] { "/api/v{version}/logs", "/api/v{version}/marketing/attribution-events", "/api/v{version}/marketing/attribution-events/signup" }) {
            Assert.True(sdk.RootElement.GetProperty("paths").TryGetProperty(path, out _));
            Assert.False(published.RootElement.GetProperty("paths").TryGetProperty(path, out _));
        }
    }

    private WebApplicationFactory<Program> CreateSdkFactory() => factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
        // Only the generation view includes internal telemetry and CLR nullable metadata.
        services.Configure<MvcOptions>(options => options.Conventions.Add(new SdkApiVisibilityConvention()));
        services.PostConfigure<SchemaGeneratorOptions>(options => {
            options.SupportNonNullableReferenceTypes = true;
            options.UseAllOfToExtendReferenceSchemas = true;
        });
    }));

    [ExcludeFromCodeCoverage]
    private sealed class SdkApiVisibilityConvention : IApplicationModelConvention {
        public void Apply(ApplicationModel application) {
            foreach (ControllerModel controller in application.Controllers) {
                if (controller.ControllerType.FullName is not (
                    "FoodDiary.Modules.Fasting.Presentation.Features.Logs.LogsController" or
                    "FoodDiary.Modules.Marketing.Presentation.Controllers.MarketingAttributionController")) {
                    continue;
                }
                controller.ApiExplorer.IsVisible = true;
                foreach (ActionModel action in controller.Actions) {
                    action.ApiExplorer.IsVisible = true;
                }
            }
        }
    }
}

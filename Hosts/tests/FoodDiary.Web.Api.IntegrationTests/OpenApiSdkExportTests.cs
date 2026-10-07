using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class OpenApiSdkExportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Fact]
    public async Task ExportOpenApiAsync() {
        await using WebApplicationFactory<Program> sdkFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            // Enrich the generated client's view without tightening the published v1 document.
            services.PostConfigure<SchemaGeneratorOptions>(options => {
                options.SupportNonNullableReferenceTypes = true;
                options.UseAllOfToExtendReferenceSchemas = true;
            });
        }));
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
        await using WebApplicationFactory<Program> sdkFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.PostConfigure<SchemaGeneratorOptions>(options => {
                options.SupportNonNullableReferenceTypes = true;
                options.UseAllOfToExtendReferenceSchemas = true;
            });
        }));
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
}

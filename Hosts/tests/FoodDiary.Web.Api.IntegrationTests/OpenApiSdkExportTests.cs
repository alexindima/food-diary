using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class OpenApiSdkExportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Fact]
    public async Task ExportOpenApiAsync() {
        using HttpClient client = factory.CreateClient();
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
}

using FoodDiary.Application.Runtime.Common.Services;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleTelemetryOwnershipTests {
    [Fact]
    public void ResolveModule_RecognizesAnActualModuleRequestAssembly() =>
        Assert.Equal("Products", ModuleOperationTelemetry.ResolveModule(
            typeof(FoodDiary.Modules.Products.Application.Queries.GetProducts.GetProductsQuery).Assembly.GetName().Name));

    [Fact]
    public void ResolveModule_RecognizesAnOwnerContractsRequestAssembly() =>
        Assert.Equal("Ai", ModuleOperationTelemetry.ResolveModule(
            typeof(FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt.UpsertAiPromptCommand).Assembly.GetName().Name));
}

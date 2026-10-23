using FoodDiary.Modules.Ai.Contracts.Commands.UpsertAiPrompt;
using FoodDiary.Modules.Products.Application.Queries.GetProducts;
using FoodDiary.Application.Runtime.Common.Services;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class ModuleTelemetryOwnershipTests {
    [Fact]
    public void ResolveModule_RecognizesAnActualModuleRequestAssembly() =>
        Assert.Equal("Products", ModuleOperationTelemetry.ResolveModule(
            typeof(GetProductsQuery).Assembly.GetName().Name));

    [Fact]
    public void ResolveModule_RecognizesAnOwnerContractsRequestAssembly() =>
        Assert.Equal("Ai", ModuleOperationTelemetry.ResolveModule(
            typeof(UpsertAiPromptCommand).Assembly.GetName().Name));
}

using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Integrations.Services;
using FoodDiary.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class TelegramRecognitionProvider {
    internal static void Configure(IServiceCollection services) {
        IOpenAiFoodClient ai = Substitute.For<IOpenAiFoodClient>();
        ai.GetAnalyzeFoodImageTokenBudgetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new AiProviderTokenBudget(10, 20)));
        ai.GetCalculateNutritionTokenBudgetAsync(Arg.Any<IReadOnlyList<FoodVisionItemModel>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new AiProviderTokenBudget(10, 20)));
        var vision = new FoodVisionModel([new FoodVisionItemModel("Apple", "Apple", 100, "g", 0.9m)]);
        ai.AnalyzeFoodImageAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                Assert.StartsWith("data:image/png;base64,", call.ArgAt<string>(0), StringComparison.Ordinal);
                return Result.Success(new OpenAiFoodClientResponse<FoodVisionModel>(vision, "vision", "test-model", new AiUsageTokens(10, 5, 15)));
            });
        var nutrition = new FoodNutritionModel(52, 0, 0, 14, 2, 0,
            [new FoodNutritionItemModel("Apple", 100, "g", 52, 0, 0, 14, 2, 0)]);
        ai.CalculateNutritionAsync(Arg.Any<IReadOnlyList<FoodVisionItemModel>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new OpenAiFoodClientResponse<FoodNutritionModel>(nutrition, "nutrition", "test-model", new AiUsageTokens(10, 5, 15))));
        services.Replace(ServiceDescriptor.Singleton(ai));

        byte[] png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
        IObjectStorageClient storage = Substitute.For<IObjectStorageClient>();
        storage.GetObjectInfoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StoredObjectInfo(png.Length, "image/png"));
        storage.GetObjectBytesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(png);
        services.Replace(ServiceDescriptor.Singleton(storage));
    }
}

using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Services.DiaryPdf;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class DiaryPdfGeneratorTests {
    [Fact]
    public async Task GenerateAsync_WithMeals_ReturnsPdfDocument() {
        var userId = UserId.New();
        var meals = new[] {
            CreateMeal(userId, new DateTime(2026, 5, 2, 21, 4, 0, DateTimeKind.Utc), 946, 59, 45, 76, 7),
            CreateMeal(userId, new DateTime(2026, 5, 3, 20, 41, 0, DateTimeKind.Utc), 905, 58, 45, 66, 5),
            CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3),
        };
        var generator = new DiaryPdfGenerator();

        var pdf = await generator.GenerateAsync(
            meals,
            new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            null,
            240,
            "https://дневникеды.рф",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithMealImage_ReturnsPdfDocument() {
        const string transparentPngDataUrl =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";
        var userId = UserId.New();
        var meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage(transparentPngDataUrl);
        meal.UpdateSatietyLevels(2, 5);
        var generator = new DiaryPdfGenerator();

        var pdf = await generator.GenerateAsync(
            [meal],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            null,
            240,
            "https://fooddiary.club",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithIngredientImagesAndNoMealImage_DownloadsCollageSources() {
        var userId = UserId.New();
        var meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 410, 12, 10, 40, 6);
        AddProductItem(meal, CreateProduct(userId, "Rice", "https://example.test/rice.png"), 150);
        AddProductItem(meal, CreateProduct(userId, "Carrot", "https://example.test/carrot.png"), 80);
        var imageHandler = new RecordingImageHandler(successfulImageResponse: true);
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        var pdf = await generator.GenerateAsync(
            [meal],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            null,
            240,
            null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(2, imageHandler.RequestCount);
        Assert.Contains("https://example.test/rice.png", imageHandler.RequestedUrls);
        Assert.Contains("https://example.test/carrot.png", imageHandler.RequestedUrls);
    }

    [Fact]
    public async Task GenerateAsync_WithRussianLocaleTimeZoneAndUnicodeOrigin_ReturnsPdfDocument() {
        var userId = UserId.New();
        var meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        var generator = new DiaryPdfGenerator();

        var pdf = await generator.GenerateAsync(
            [meal],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            "ru",
            240,
            "https://дневникеды.рф",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithLongPeriod_DoesNotDownloadMealImages() {
        var userId = UserId.New();
        var meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage("https://example.test/meal.jpg");
        AddProductItem(meal, CreateProduct(userId, "Rice", "https://example.test/rice.png"), 150);
        var imageHandler = new RecordingImageHandler();
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        var pdf = await generator.GenerateAsync(
            [meal],
            new DateTime(2026, 4, 20, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            null,
            240,
            null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(0, imageHandler.RequestCount);
    }

    private static Meal CreateMeal(
        UserId userId,
        DateTime date,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber) {
        var meal = Meal.Create(userId, date, MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(calories, proteins, fats, carbs, fiber, 0, IsAutoCalculated: true));
        return meal;
    }

    private static Product CreateProduct(UserId userId, string name, string imageUrl) =>
        Product.Create(
            userId,
            name,
            MeasurementUnit.G,
            100,
            100,
            caloriesPerBase: 120,
            proteinsPerBase: 3,
            fatsPerBase: 1,
            carbsPerBase: 20,
            fiberPerBase: 2,
            alcoholPerBase: 0,
            imageUrl: imageUrl);

    private static void AddProductItem(Meal meal, Product product, double amount) {
        var item = meal.AddProduct(product.Id, amount);
        typeof(MealItem)
            .GetProperty(nameof(MealItem.Product))!
            .SetValue(item, product);
    }

    private sealed class RecordingImageHandler : HttpMessageHandler {
        private const string TransparentPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";

        private readonly bool _successfulImageResponse;

        public RecordingImageHandler(bool successfulImageResponse = false) {
            _successfulImageResponse = successfulImageResponse;
        }

        public int RequestCount { get; private set; }
        public IReadOnlyList<string> RequestedUrls => _requestedUrls;

        private readonly List<string> _requestedUrls = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestCount++;
            _requestedUrls.Add(request.RequestUri?.ToString() ?? "");

            if (!_successfulImageResponse) {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new ByteArrayContent(Convert.FromBase64String(TransparentPngBase64))
            };
            return Task.FromResult(response);
        }
    }
}

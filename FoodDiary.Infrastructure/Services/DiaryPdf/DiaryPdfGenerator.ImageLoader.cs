using FoodDiary.Application.Abstractions.Meals.Models;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private async Task<IReadOnlyDictionary<Guid, byte[]>> LoadMealImagesAsync(
        IReadOnlyList<MealConsumptionReadModel> meals,
        CancellationToken cancellationToken) {
        using var gate = new SemaphoreSlim(MaxParallelMealImageDownloads);
        var cache = new Dictionary<string, Lazy<Task<byte[]?>>>(StringComparer.Ordinal);
        Task<MealImageEntry>[] tasks = [.. meals
            .Take(MaxMealImagesPerReport)
            .Select(meal => LoadMealImageEntryAsync(meal, cache, gate, cancellationToken))];

        MealImageEntry[] entries = await Task.WhenAll(tasks).ConfigureAwait(false);

        return entries
            .Where(entry => entry.Image is not null)
            .ToDictionary(entry => entry.MealId, entry => entry.Image!);
    }

    private async Task<MealImageEntry> LoadMealImageEntryAsync(
        MealConsumptionReadModel meal,
        Dictionary<string, Lazy<Task<byte[]?>>> cache,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        byte[]? image = await LoadMealImageForReportAsync(meal, cache, gate, cancellationToken).ConfigureAwait(false);
        return new MealImageEntry(meal.Id, image);
    }

    private static bool ShouldUseCompactMealsMode(DateTime dateFrom, DateTime dateTo) =>
        GetReportDayCount(dateFrom, dateTo) > 7;

    private static int GetReportDayCount(DateTime dateFrom, DateTime dateTo) {
        DateTime normalizedFrom = EnsureUtcForReport(dateFrom);
        DateTime normalizedTo = EnsureUtcForReport(dateTo);
        if (normalizedTo < normalizedFrom) {
            (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
        }

        TimeSpan duration = normalizedTo - normalizedFrom;
        return Math.Clamp((int)Math.Ceiling(duration.TotalDays), 1, 366);
    }

    private static DateTime EnsureUtcForReport(DateTime value) =>
        value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    private async Task<byte[]?> LoadMealImageForReportAsync(
        MealConsumptionReadModel meal,
        Dictionary<string, Lazy<Task<byte[]?>>> cache,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(meal.ImageUrl)) {
            return await LoadCachedMealImageAsync(meal.ImageUrl, cache, gate, cancellationToken).ConfigureAwait(false);
        }

        byte[]?[] compositionImages = await Task.WhenAll(
            GetCompositionImageUrls(meal)
                .Take(MaxIngredientImagesPerCollage)
                .Select(imageUrl => LoadCachedMealImageAsync(imageUrl, cache, gate, cancellationToken))).ConfigureAwait(false);

        return CreateMealImageCollage(compositionImages.Where(image => image is not null).Cast<byte[]>().ToArray());
    }

    private async Task<byte[]?> LoadCachedMealImageAsync(
        string imageUrl,
        Dictionary<string, Lazy<Task<byte[]?>>> cache,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        Lazy<Task<byte[]?>> cached;
        lock (cache) {
            if (!cache.TryGetValue(imageUrl, out cached!)) {
                cached = new Lazy<Task<byte[]?>>(
                    () => LoadMealImageWithGateAsync(imageUrl, gate, cancellationToken),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                cache[imageUrl] = cached;
            }
        }

        return await cached.Value.ConfigureAwait(false);
    }

    private async Task<byte[]?> LoadMealImageWithGateAsync(
        string imageUrl,
        SemaphoreSlim gate,
        CancellationToken cancellationToken) {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            return await LoadMealImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
        } finally {
            gate.Release();
        }
    }

    private async Task<byte[]?> LoadMealImageAsync(string imageUrl, CancellationToken cancellationToken) {
        try {
            if (TryReadDataUrl(imageUrl, out byte[] dataUrlBytes)) {
                return PrepareMealImage(dataUrlBytes);
            }

            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri) ||
                !await IsAllowedRemoteImageUriAsync(uri, cancellationToken).ConfigureAwait(false)) {
                return null;
            }

            using HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxMealImageBytes) {
                return null;
            }

            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            byte[] downloaded;
            await using (stream.ConfigureAwait(false)) {
                var memory = new MemoryStream();
                await using (memory.ConfigureAwait(false)) {
                    byte[] buffer = new byte[81920];
                    int read;

                    while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
                        await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                        if (memory.Length > MaxMealImageBytes) {
                            return null;
                        }
                    }

                    downloaded = memory.ToArray();
                }
            }

            return downloaded.Length == 0 ? null : PrepareMealImage(downloaded);
        } catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException or FormatException or IOException or SocketException) {
            return null;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct MealImageEntry(Guid MealId, byte[]? Image);

    private static IReadOnlyList<string> GetCompositionImageUrls(MealConsumptionReadModel meal) =>
        meal.Items
            .OrderBy(item => item.Id)
            .Select(item => item.ProductImageUrl ?? item.RecipeImageUrl)
            .Concat(meal.AiSessions
                .OrderBy(session => session.RecognizedAtUtc)
                .Select(session => session.ImageUrl))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
}

namespace FoodDiary.Application.Abstractions.Common.Validation;

public static class PaginationPolicy {
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageNumber = 10_000;
    public const int MaxPageSize = 100;
    public const int MaxCollectionSize = 1_000;

    public static int NormalizePage(int page) =>
        Math.Clamp(page, DefaultPage, MaxPageNumber);

    public static int NormalizePageSize(
        int pageSize,
        int defaultPageSize = DefaultPageSize,
        int maxPageSize = MaxPageSize) {
        ValidatePageSizeBounds(defaultPageSize, maxPageSize);

        return pageSize <= 0
            ? defaultPageSize
            : Math.Min(pageSize, maxPageSize);
    }

    public static int NormalizeCollectionLimit(int? limit) =>
        NormalizePageSize(limit ?? MaxCollectionSize, MaxCollectionSize, MaxCollectionSize);

    public static int NormalizePageSizeOrDefault(
        int pageSize,
        int defaultPageSize = DefaultPageSize,
        int maxPageSize = MaxPageSize) {
        ValidatePageSizeBounds(defaultPageSize, maxPageSize);
        return pageSize is >= 1 && pageSize <= maxPageSize
            ? pageSize
            : defaultPageSize;
    }

    private static void ValidatePageSizeBounds(int defaultPageSize, int maxPageSize) {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPageSize, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultPageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(defaultPageSize, maxPageSize);
    }
}

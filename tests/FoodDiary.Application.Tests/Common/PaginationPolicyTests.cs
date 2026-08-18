using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Tests.Common;

[ExcludeFromCodeCoverage]
public sealed class PaginationPolicyTests {
    [Theory]
    [InlineData(int.MinValue, PaginationPolicy.DefaultPage)]
    [InlineData(1, 1)]
    [InlineData(42, 42)]
    [InlineData(int.MaxValue, PaginationPolicy.MaxPageNumber)]
    public void NormalizePage_ClampsToSupportedRange(int value, int expected) {
        Assert.Equal(expected, PaginationPolicy.NormalizePage(value));
    }

    [Theory]
    [InlineData(int.MinValue, 20)]
    [InlineData(1, 1)]
    [InlineData(42, 42)]
    [InlineData(int.MaxValue, PaginationPolicy.MaxPageSize)]
    public void NormalizePageSize_ClampsToSupportedRange(int value, int expected) {
        Assert.Equal(expected, PaginationPolicy.NormalizePageSize(value));
    }

    [Fact]
    public void NormalizePageSize_WithFeatureSpecificBounds_UsesThoseBounds() {
        Assert.Multiple(
            () => Assert.Equal(10, PaginationPolicy.NormalizePageSize(0, defaultPageSize: 10, maxPageSize: 50)),
            () => Assert.Equal(50, PaginationPolicy.NormalizePageSize(500, defaultPageSize: 10, maxPageSize: 50)));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(101, 100)]
    [InlineData(1, 0)]
    public void NormalizePageSize_WithInvalidBounds_Throws(int defaultPageSize, int maxPageSize) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PaginationPolicy.NormalizePageSize(10, defaultPageSize, maxPageSize));
    }

    [Theory]
    [InlineData(null, PaginationPolicy.MaxCollectionSize)]
    [InlineData(25, 25)]
    [InlineData(int.MaxValue, PaginationPolicy.MaxCollectionSize)]
    public void NormalizeCollectionLimit_UsesBoundedDefault(int? value, int expected) {
        Assert.Equal(expected, PaginationPolicy.NormalizeCollectionLimit(value));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(25, 25)]
    [InlineData(101, 20)]
    public void NormalizePageSizeOrDefault_InvalidValuesUseDefault(int value, int expected) {
        Assert.Equal(expected, PaginationPolicy.NormalizePageSizeOrDefault(value));
    }
}

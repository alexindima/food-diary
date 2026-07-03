using System.Globalization;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class PagedHttpResponseMappingsTests {
    [Fact]
    public void ToPagedHttpResponse_MapsItemsAndPagingMetadata() {
        var response = new PagedResponse<int>(
            [1, 2, 3],
            Page: 2,
            Limit: 25,
            TotalPages: 4,
            TotalItems: 87);

        var result = response.ToPagedHttpResponse(static value => string.Create(CultureInfo.InvariantCulture, $"item-{value}"));

        Assert.Multiple(
            () => Assert.Equal(["item-1", "item-2", "item-3"], result.Data),
            () => Assert.Equal(2, result.Page),
            () => Assert.Equal(25, result.Limit),
            () => Assert.Equal(4, result.TotalPages),
            () => Assert.Equal(87, result.TotalItems));
    }

    [Fact]
    public void ToHttpResponseList_MapsItemsToList() {
        int[] items = [2, 4, 6];

        IReadOnlyList<string> result = items.ToHttpResponseList(static value => string.Create(CultureInfo.InvariantCulture, $"value-{value}"));

        Assert.Equal(["value-2", "value-4", "value-6"], result);
    }
}

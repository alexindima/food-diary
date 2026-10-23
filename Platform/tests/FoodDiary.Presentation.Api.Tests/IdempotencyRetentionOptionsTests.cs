using MsOptions = Microsoft.Extensions.Options.Options;
using FoodDiary.Presentation.Api.Filters;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class IdempotencyRetentionOptionsTests {
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void ExplicitResponseRetention_UsesThePositiveSevenDayConfigurationBoundary(int days, bool expectedValid) {
        var options = new IdempotencyFilterOptions { ResponseTtl = TimeSpan.FromDays(days) };
        IIdempotencyStore store = Substitute.For<IIdempotencyStore>();

        bool valid = IdempotencyFilterOptions.IsValid(options);

        Assert.Equal(expectedValid, valid);
        if (expectedValid) {
            Assert.NotNull(new IdempotencyFilter(store, idempotencyOptions: MsOptions.Create(options)));
        } else {
            Assert.Throws<InvalidOperationException>(() =>
                new IdempotencyFilter(store, idempotencyOptions: MsOptions.Create(options)));
        }
        Assert.Empty(store.ReceivedCalls());
    }
}

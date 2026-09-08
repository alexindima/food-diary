using FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessagePage;
using FluentValidation.Results;

namespace FoodDiary.MailInbox.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MessagePageValidationTests {
    [Theory]
    [InlineData(0, 50, false)]
    [InlineData(-1, 50, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 201, false)]
    [InlineData(1, 50, true)]
    [InlineData(int.MaxValue, 200, true)]
    public async Task ValidateAsync_EnforcesPageAndSizeBounds(int page, int limit, bool expected) {
        var validator = new GetInboundMailMessagePageQueryValidator();
        ValidationResult result = await validator.ValidateAsync(new GetInboundMailMessagePageQuery(page, limit));
        Assert.Equal(expected, result.IsValid);
    }
}

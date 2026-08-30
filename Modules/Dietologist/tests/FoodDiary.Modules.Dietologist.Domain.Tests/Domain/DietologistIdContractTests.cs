using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DietologistIdContractTests {
    [Fact]
    public void DietologistIds_PreserveGuidAcrossConversionsAndFormatting() {
        var value = Guid.Parse("a799c862-94ef-43e7-9d58-f54c64e713df");

        AssertId((ClientTaskId)value, value, static id => id.Value, static id => id.ToString(), static id => id);
        AssertId((DietologistInvitationId)value, value, static id => id.Value, static id => id.ToString(), static id => id);
        AssertId((RecommendationBulkDispatchId)value, value, static id => id.Value, static id => id.ToString(), static id => id);
        AssertId((RecommendationCommentId)value, value, static id => id.Value, static id => id.ToString(), static id => id);
        AssertId((RecommendationId)value, value, static id => id.Value, static id => id.ToString(), static id => id);
        AssertId((RecommendationTemplateId)value, value, static id => id.Value, static id => id.ToString(), static id => id);
    }

    [Fact]
    public void DietologistIds_ExposeEmptySentinelWhereDefined() {
        Assert.Multiple(
            () => Assert.Equal(Guid.Empty, DietologistInvitationId.Empty.Value),
            () => Assert.Equal(Guid.Empty, RecommendationBulkDispatchId.Empty.Value),
            () => Assert.Equal(Guid.Empty, RecommendationTemplateId.Empty.Value));
    }

    private static void AssertId<T>(
        T id,
        Guid expected,
        Func<T, Guid> value,
        Func<T, string> format,
        Func<T, Guid> implicitConversion) {
        Assert.Multiple(
            () => Assert.Equal(expected, value(id)),
            () => Assert.Equal(expected.ToString(), format(id)),
            () => Assert.Equal(expected, implicitConversion(id)));
    }
}

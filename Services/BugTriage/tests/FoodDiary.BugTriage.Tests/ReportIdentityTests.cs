using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports.Identifiers;

namespace FoodDiary.BugTriage.Tests;

[ExcludeFromCodeCoverage]
public sealed class ReportIdentityTests {
    [Fact]
    public void LeaseOperations_RequireDistinctReportAndTokenIdentities() {
        Type[] parameters = [.. typeof(IBugReportStore).GetMethod(nameof(IBugReportStore.GetMimeAsync))!
            .GetParameters().Select(parameter => parameter.ParameterType)];
        Assert.Equal(typeof(BugReportId), parameters[0]);
        Assert.Equal(typeof(LeaseToken), parameters[1]);
        Assert.Equal(typeof(SourceMessageId), typeof(IBugReportStore).GetMethod(nameof(IBugReportStore.ContainsAsync))!
            .GetParameters()[0].ParameterType);
    }

    [Fact]
    public void Identities_KeepGuidStorageValuesAndHaveNoCrossOwnerImplicitConversion() {
        var value = Guid.NewGuid();
        Assert.Equal(value, ((BugReportId)value).Value);
        Assert.Equal(value, ((SourceMessageId)value).Value);
        Assert.Equal(value, ((LeaseToken)value).Value);
        Assert.DoesNotContain(typeof(LeaseToken).GetMethods(), method => string.Equals(method.Name, "op_Implicit", StringComparison.Ordinal));
    }
}

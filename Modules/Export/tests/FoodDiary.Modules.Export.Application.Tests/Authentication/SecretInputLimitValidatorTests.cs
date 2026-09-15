using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Export.Application.Models;
using FoodDiary.Modules.Export.Application.Queries.ExportCycle;

namespace FoodDiary.Modules.Export.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class SecretInputLimitValidatorTests {
    private static readonly string OversizedPassword =
        new('p', AuthenticationInputLimits.MaximumPasswordLength + 1);

    [Fact]
    public void SensitiveCycleExportValidator_RejectsOversizedCurrentPassword() {
        var date = new DateOnly(2026, 8, 19);
        var query = new ExportCycleQuery(
            Guid.NewGuid(),
            date,
            date,
            Scope: CycleExportScope.Sensitive,
            CurrentPassword: OversizedPassword);

        new ExportCycleQueryValidator()
            .TestValidate(query)
            .ShouldHaveValidationErrorFor(value => value.CurrentPassword);
    }
}

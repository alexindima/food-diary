using FoodDiary.Modules.Admin.Application.Commands.ImportAdminDailyAdvices;

namespace FoodDiary.Modules.Admin.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImportAdminDailyAdvicesValidatorTests {
    [Fact]
    public void RejectsUnsupportedVersionEmptyNullAndOversizedBatches() {
        var validator = new ImportAdminDailyAdvicesCommandValidator();
        ImportAdminDailyAdvicesCommand[] invalid = [new(2, [new("Advice", "en")]), new(1, []), new(1, null!), new(1, [null!]),
            new(1, Enumerable.Repeat(new ImportAdminDailyAdviceItem("Advice", "en"), 1001).ToArray())];
        foreach (ImportAdminDailyAdvicesCommand command in invalid) {
            Assert.False(validator.Validate(command).IsValid);
        }
        Assert.True(validator.Validate(new ImportAdminDailyAdvicesCommand(1, [new("Advice", "en")])).IsValid);
    }
}

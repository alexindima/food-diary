using FluentValidation.TestHelper;
using FoodDiary.Modules.Cycles.Application.Commands.UpdateMenstrualEpisode;

namespace FoodDiary.Modules.Cycles.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MenstrualEpisodeDateValidationTests {
    [Fact]
    public async Task EndBeforeStart_UsesClientValidationError() {
        TestValidationResult<UpdateMenstrualEpisodeCommand> result = await new UpdateMenstrualEpisodeCommandValidator()
            .TestValidateAsync(CreateCommand(new DateOnly(2026, 4, 1)));
        result.ShouldHaveValidationErrorFor(command => command.EndDate).WithErrorCode("Validation.Invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task OpenSameDayAndLaterEnd_AreValid(int? endDay) {
        DateOnly? end = endDay.HasValue ? new DateOnly(2026, 4, endDay.Value) : null;
        TestValidationResult<UpdateMenstrualEpisodeCommand> result = await new UpdateMenstrualEpisodeCommandValidator()
            .TestValidateAsync(CreateCommand(end));
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static UpdateMenstrualEpisodeCommand CreateCommand(DateOnly? end) =>
        new(UserId: Guid.NewGuid(), CycleProfileId: Guid.NewGuid(), MenstrualEpisodeId: Guid.NewGuid(),
            StartDate: new DateOnly(2026, 4, 2), EndDate: end);
}

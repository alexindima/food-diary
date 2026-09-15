using FluentValidation;

namespace FoodDiary.Modules.Cycles.Application.Commands.UpdateMenstrualEpisode;

public sealed class UpdateMenstrualEpisodeCommandValidator : AbstractValidator<UpdateMenstrualEpisodeCommand> {
    public UpdateMenstrualEpisodeCommandValidator() {
        RuleFor(command => command.CycleProfileId).NotEmpty();
        RuleFor(command => command.MenstrualEpisodeId).NotEmpty();
        RuleFor(command => command.StartDate).NotEmpty();
        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate)
            .When(command => command.EndDate.HasValue);
    }
}

using FluentValidation;

namespace FoodDiary.Application.Cycles.Commands.DeleteMenstrualEpisode;

public sealed class DeleteMenstrualEpisodeCommandValidator : AbstractValidator<DeleteMenstrualEpisodeCommand> {
    public DeleteMenstrualEpisodeCommandValidator() {
        RuleFor(command => command.CycleProfileId).NotEmpty();
        RuleFor(command => command.MenstrualEpisodeId).NotEmpty();
    }
}

using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.UpdateAdminLesson;

public sealed class UpdateAdminLessonCommandValidator : AbstractValidator<UpdateAdminLessonCommand> {
    public UpdateAdminLessonCommandValidator() {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Lesson ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Title is required.")
            .MaximumLength(256)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Title must be at most 256 characters.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Content is required.")
            .MaximumLength(65536)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Content must be at most 65536 characters.");

        RuleFor(x => x.Locale)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Locale is required.")
            .MaximumLength(10)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Locale must be at most 10 characters.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Category is required.");

        RuleFor(x => x.Difficulty)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Difficulty is required.");

        RuleFor(x => x.EstimatedReadMinutes)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Estimated read minutes must be at least 1.");
    }
}

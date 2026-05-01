using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.ImportAdminLessons;

public sealed class ImportAdminLessonsCommandValidator : AbstractValidator<ImportAdminLessonsCommand> {
    public ImportAdminLessonsCommandValidator() {
        RuleFor(x => x.Version)
            .Equal(1)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Unsupported lesson import format version.");

        RuleFor(x => x.Lessons)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("At least one lesson is required.")
            .Must(lessons => lessons.Count <= 100)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("A lesson import file can contain at most 100 lessons.");

        RuleForEach(x => x.Lessons).ChildRules(lesson => {
            lesson.RuleFor(x => x.Title)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("Title is required.")
                .MaximumLength(256)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Title must be at most 256 characters.");

            lesson.RuleFor(x => x.Content)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("Content is required.")
                .MaximumLength(65536)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Content must be at most 65536 characters.");

            lesson.RuleFor(x => x.Summary)
                .MaximumLength(512)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Summary must be at most 512 characters.");

            lesson.RuleFor(x => x.Locale)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("Locale is required.")
                .MaximumLength(10)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Locale must be at most 10 characters.");

            lesson.RuleFor(x => x.Category)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("Category is required.");

            lesson.RuleFor(x => x.Difficulty)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("Difficulty is required.");

            lesson.RuleFor(x => x.EstimatedReadMinutes)
                .GreaterThanOrEqualTo(1)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Estimated read minutes must be at least 1.");

            lesson.RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Sort order must be zero or greater.");
        });
    }
}

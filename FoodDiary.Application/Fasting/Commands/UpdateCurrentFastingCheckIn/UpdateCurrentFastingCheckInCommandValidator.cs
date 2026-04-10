using FluentValidation;

namespace FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;

public class UpdateCurrentFastingCheckInCommandValidator : AbstractValidator<UpdateCurrentFastingCheckInCommand> {
    private static readonly string[] AllowedSymptoms = [
        "headache",
        "weakness",
        "irritability",
        "dizziness",
        "cravings",
        "good"
    ];

    public UpdateCurrentFastingCheckInCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.HungerLevel)
            .InclusiveBetween(1, 5)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Hunger level must be between 1 and 5.");

        RuleFor(x => x.EnergyLevel)
            .InclusiveBetween(1, 5)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Energy level must be between 1 and 5.");

        RuleFor(x => x.MoodLevel)
            .InclusiveBetween(1, 5)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Mood level must be between 1 and 5.");

        RuleForEach(x => x.Symptoms)
            .Must(symptom => !string.IsNullOrWhiteSpace(symptom) && AllowedSymptoms.Contains(symptom.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Unknown fasting symptom value.");

        RuleFor(x => x.Symptoms)
            .Must(symptoms => symptoms is null || symptoms.Count <= 8)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("A maximum of 8 symptoms is allowed.");

        RuleFor(x => x.CheckInNotes)
            .MaximumLength(500)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Check-in notes must be at most 500 characters.");
    }
}

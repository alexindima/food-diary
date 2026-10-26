namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

public enum PasswordLoginAttemptOutcome {
    Abandoned = 0,
    InvalidCredentials = 1,
    Succeeded = 2,
}

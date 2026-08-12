namespace FoodDiary.Application.Abstractions.Users.Models;

public enum UserPasswordResetIssueStatus {
    NotEligible = 0,
    Throttled = 1,
    Issued = 2,
}

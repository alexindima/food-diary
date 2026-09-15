namespace FoodDiary.Modules.Users.Contracts.Models;

public enum UserPasswordResetIssueStatus {
    NotEligible = 0,
    Throttled = 1,
    Issued = 2,
}

namespace FoodDiary.Modules.Identity.Contracts.Authentication.Commands.BootstrapInitialAdmin;

public enum BootstrapInitialAdminStatus {
    SkippedMissingPassword = 0,
    SkippedExistingUser = 1,
    Created = 2,
}

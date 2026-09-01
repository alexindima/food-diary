namespace FoodDiary.Application.Identity.Authentication.Commands.BootstrapInitialAdmin;

public sealed record BootstrapInitialAdminModel(
    BootstrapInitialAdminStatus Status,
    string Email);

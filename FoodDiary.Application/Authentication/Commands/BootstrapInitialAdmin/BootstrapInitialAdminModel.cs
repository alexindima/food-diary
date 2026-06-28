namespace FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;

public sealed record BootstrapInitialAdminModel(
    BootstrapInitialAdminStatus Status,
    string Email);

namespace FoodDiary.Contracts.Authentication;

public record RestoreAccountRequest(
    string Email,
    string Password
);

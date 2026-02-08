namespace FoodDiary.Contracts.Authentication;

public record RegisterRequest(
    string Email,
    string Password,
    string? Language
);

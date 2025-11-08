namespace FoodDiary.Contracts.Users;

public record UserResponse(
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? Weight,
    double? Height,
    string? ProfileImage,
    bool IsActive
);

using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher {
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(password, hashedPassword);
}

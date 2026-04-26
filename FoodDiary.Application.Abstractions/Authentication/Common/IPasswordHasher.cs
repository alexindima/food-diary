namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IPasswordHasher {
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
}

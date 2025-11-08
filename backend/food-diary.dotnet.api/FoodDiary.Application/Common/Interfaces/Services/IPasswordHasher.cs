namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
}

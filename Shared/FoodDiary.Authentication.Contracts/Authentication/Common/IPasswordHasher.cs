namespace FoodDiary.Authentication.Contracts.Authentication.Common;

public interface IPasswordHasher {
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
    bool NeedsRehash(string hashedPassword) => false;
}

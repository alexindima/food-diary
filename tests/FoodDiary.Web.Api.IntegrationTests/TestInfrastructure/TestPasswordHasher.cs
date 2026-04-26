using FoodDiary.Application.Authentication.Common;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

public sealed class TestPasswordHasher : IPasswordHasher {
    private const string Prefix = "test-hash:";

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string hashedPassword) =>
        string.Equals(Hash(password), hashedPassword, StringComparison.Ordinal);
}

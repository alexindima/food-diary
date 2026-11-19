using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Queries.GetUserRegistrationTime;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserRegistrationTime;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserRegistrationLookupCompatibilityTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingLookupAdaptersRetainRegistrationTimeThroughTheDefaultInterfaceMethod(bool exists) {
        var user = User.Create("registration-adapter@example.test", "hash");
        IUserLookupRepository users = new ExistingLookup(user);
        UserId id = exists ? user.Id : UserId.New();

        DateTime? registered = await new GetUserRegistrationTimeQueryHandler(users)
            .Handle(new GetUserRegistrationTimeQuery(id), CancellationToken.None);

        Assert.Equal(exists ? user.CreatedOnUtc : (DateTime?)null, registered);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ExistingLookup(User user) : IUserLookupRepository {
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(id == user.Id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

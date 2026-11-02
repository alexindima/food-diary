using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class ContentRetentionCompositionTests {
    [Fact]
    public void DuplicateRetentionOrdering_FailsBeforeAnyAccountCanBePurged() {
        IUserContentRetentionParticipant first = Substitute.For<IUserContentRetentionParticipant>();
        IUserContentRetentionParticipant second = Substitute.For<IUserContentRetentionParticipant>();
        first.Order.Returns(10);
        second.Order.Returns(10);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => new UserCleanupService(null!,
            [Substitute.For<IUserDataPurgeParticipant>()], NullLogger<UserCleanupService>.Instance,
            Substitute.For<IModuleTransactionCoordinator>(), Substitute.For<IModuleScopeGuard>(), contentRetentionParticipants: [first, second]));

        Assert.Contains("unique ordering", failure.Message, StringComparison.Ordinal);
    }
}

using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void AdminMailInboxErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Admin.Application.Abstractions", typeof(AdminMailInboxErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Admin.Common", typeof(AdminMailInboxErrors).Namespace));
    }

    [Fact]
    public void AdminMailInboxErrors_PreservesEveryPublicErrorContract() {
        AssertError(AdminMailInboxErrors.MessageNotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "MailInbox.MessageNotFound", "Mail inbox message with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}

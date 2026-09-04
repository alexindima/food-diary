using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Application.Tests.Compatibility;

[ExcludeFromCodeCoverage]
public sealed class UsersIdentityContractAssemblyTests {
    [Theory]
    [InlineData(typeof(IUserProfileReadService), "FoodDiary.Modules.Users.Contracts")]
    [InlineData(typeof(IUserAccessTokenSecurityReader), "FoodDiary.Modules.Users.Contracts")]
    [InlineData(typeof(UserErrors), "FoodDiary.Modules.Users.Contracts")]
    [InlineData(typeof(IUserRepository), "FoodDiary.Modules.Users.Application.Abstractions")]
    [InlineData(typeof(IUserRoleCatalogService), "FoodDiary.Modules.Users.Application.Abstractions")]
    [InlineData(typeof(IUserAdminReadRepository), "FoodDiary.Modules.Users.Application.Abstractions")]
    [InlineData(typeof(IPasswordHasher), "FoodDiary.Modules.Identity.Application.Abstractions")]
    [InlineData(typeof(IEmailTemplateRepository), "FoodDiary.Modules.Identity.Application.Abstractions")]
    [InlineData(typeof(IAdminUserRoleAuditRepository), "FoodDiary.Modules.Admin.Application.Abstractions")]
    [InlineData(typeof(IBillingMarketingConversionRecorder), "FoodDiary.Modules.Billing.Application.Abstractions")]
    [InlineData(typeof(DietologistRequiredIdParser), "FoodDiary.Application.Dietologist")]
    [InlineData(typeof(DietologistEnumValueParser), "FoodDiary.Application.Dietologist")]
    [InlineData(typeof(CurrentUserAccessResolver), "FoodDiary.Application.Abstractions")]
    [InlineData(typeof(IAdminSsoCodeStore), "FoodDiary.Application.Abstractions")]
    public void ExistingSourceContracts_ResolveToTheirOwner(Type type, string expectedAssembly) {
        Assert.Equal(expectedAssembly, type.Assembly.GetName().Name);
    }
}

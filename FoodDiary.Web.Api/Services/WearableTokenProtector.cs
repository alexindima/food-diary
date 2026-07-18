using FoodDiary.Application.Abstractions.Wearables.Common;
using Microsoft.AspNetCore.DataProtection;

namespace FoodDiary.Web.Api.Services;

public sealed class WearableTokenProtector(IDataProtectionProvider dataProtectionProvider) : IWearableTokenProtector {
    private const string Purpose = "FoodDiary.Wearables.OAuthTokens.v1";
    private const string ProtectedPrefix = "fdp1:";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public bool IsProtected(string token) => token.StartsWith(ProtectedPrefix, StringComparison.Ordinal);

    public string Protect(string token) => IsProtected(token) ? token : ProtectedPrefix + _protector.Protect(token);

    public string Unprotect(string protectedToken) => IsProtected(protectedToken)
        ? _protector.Unprotect(protectedToken[ProtectedPrefix.Length..])
        : protectedToken;
}

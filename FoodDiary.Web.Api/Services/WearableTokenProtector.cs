using FoodDiary.Application.Abstractions.Wearables.Common;
using Microsoft.AspNetCore.DataProtection;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Web.Api.Services;

public sealed class WearableTokenProtector(IDataProtectionProvider dataProtectionProvider) : IWearableTokenProtector {
    private const string Purpose = "FoodDiary.Wearables.OAuthTokens.v1";
    private const string ProtectedPrefix = "fdp1:";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public ProtectedWearableToken Protect(string token) =>
        ProtectedWearableToken.FromProtectedValue(ProtectedPrefix + _protector.Protect(token));

    public string Unprotect(ProtectedWearableToken protectedToken) => protectedToken.IsProtected
        ? _protector.Unprotect(protectedToken.Value[ProtectedPrefix.Length..])
        : protectedToken.Value;
}

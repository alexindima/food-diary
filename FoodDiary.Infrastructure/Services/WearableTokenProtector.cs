using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Domain.ValueObjects;
using Microsoft.AspNetCore.DataProtection;

namespace FoodDiary.Infrastructure.Services;

public sealed class WearableTokenProtector(IDataProtectionProvider dataProtectionProvider) : IWearableTokenProtector {
    private const string Purpose = "FoodDiary.WearableTokens.v1";
    private const string ProtectedPrefix = "fdp1:";
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(Purpose);

    public ProtectedWearableToken Protect(string token) =>
        ProtectedWearableToken.FromProtectedValue(ProtectedPrefix + _protector.Protect(token));

    public string Unprotect(ProtectedWearableToken protectedToken) => protectedToken.IsProtected
        ? _protector.Unprotect(protectedToken.Value[ProtectedPrefix.Length..])
        : protectedToken.Value;
}

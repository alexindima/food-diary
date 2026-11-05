using System.Security.Cryptography;
using System.Text;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

public sealed class TelegramStarsBotAuthenticator(IOptions<TelegramStarsOptions> options) : ITelegramStarsBotAuthenticator {
    public bool IsAuthenticated(string providedSecret) {
        string expected = options.Value.BotApiSecret;
        return expected.Length >= 16 && providedSecret.Length <= 1024 &&
            CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
                SHA256.HashData(Encoding.UTF8.GetBytes(providedSecret)));
    }
}

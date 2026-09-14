using FoodDiary.Modules.Billing.Application.Abstractions.Models;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingPublicConfigProvider {
    BillingPublicConfigModel GetPublicConfig();
}

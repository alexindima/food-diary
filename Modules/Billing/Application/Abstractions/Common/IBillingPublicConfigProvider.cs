using FoodDiary.Application.Abstractions.Billing.Models;

namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingPublicConfigProvider {
    BillingPublicConfigModel GetPublicConfig();
}

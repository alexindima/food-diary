using FoodDiary.Application.Billing.Models;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingPublicConfigProvider {
    BillingPublicConfigModel GetPublicConfig();
}

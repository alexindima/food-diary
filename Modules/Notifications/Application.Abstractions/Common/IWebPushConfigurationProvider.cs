namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface IWebPushConfigurationProvider {
    WebPushClientConfiguration GetClientConfiguration();
}

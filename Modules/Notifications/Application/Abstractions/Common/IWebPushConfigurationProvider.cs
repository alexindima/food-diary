namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface IWebPushConfigurationProvider {
    WebPushClientConfiguration GetClientConfiguration();
}

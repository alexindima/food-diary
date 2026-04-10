namespace FoodDiary.Application.Notifications.Common;

public interface IWebPushConfigurationProvider {
    WebPushClientConfiguration GetClientConfiguration();
}

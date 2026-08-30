namespace FoodDiary.Application.Notifications.Common;

internal static class WebPushEndpointHost {
    public static string Resolve(string endpoint) {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
            ? uri.Host
            : endpoint;
    }
}

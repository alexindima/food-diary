using Microsoft.AspNetCore.Builder;

namespace FoodDiary.MailRelay.Presentation.Extensions;

public static class MailRelayPresentationApplicationBuilderExtensions {
    public static WebApplication MapMailRelayPresentation(this WebApplication app) {
        app.MapControllers();
        return app;
    }
}

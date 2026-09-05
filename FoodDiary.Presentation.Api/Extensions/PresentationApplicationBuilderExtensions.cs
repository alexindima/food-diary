using Microsoft.AspNetCore.Builder;

namespace FoodDiary.Presentation.Api.Extensions;

public static class PresentationApplicationBuilderExtensions {
    extension(WebApplication app) {
        public WebApplication MapPresentationApi() {
            app.MapControllers();
            return app;
        }
    }
}

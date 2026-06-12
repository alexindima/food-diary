using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.MailInbox.Presentation.Extensions;

public static class MailInboxEndpointRouteBuilderExtensions {
    public static IEndpointRouteBuilder MapMailInboxPresentation(this IEndpointRouteBuilder app) {
        app.MapControllers();
        return app;
    }
}

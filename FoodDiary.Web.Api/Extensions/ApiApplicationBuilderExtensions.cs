using FoodDiary.Presentation.Api.Extensions;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiApplicationBuilderExtensions {
    public static WebApplication UseApiPipeline(this WebApplication app) {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI();
        } else {
            app.UseHttpsRedirection();
        }

        app.UseCors(ApiCompositionConstants.CorsPolicyName);
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseOutputCache();

        app.MapPresentationApi(ApiCompositionConstants.CorsPolicyName);

        return app;
    }
}

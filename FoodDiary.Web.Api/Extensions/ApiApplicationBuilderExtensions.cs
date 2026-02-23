using FoodDiary.Web.Api.Hubs;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiApplicationBuilderExtensions {
    public static WebApplication UseApiPipeline(this WebApplication app) {
        if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI();
        } else {
            app.UseHttpsRedirection();
        }

        app.UseCors(ApiCompositionConstants.CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<EmailVerificationHub>("/hubs/email-verification")
            .RequireCors(ApiCompositionConstants.CorsPolicyName);

        return app;
    }
}

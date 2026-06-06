using System.Globalization;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodDiary.Web.Api.Swagger;

public sealed class StandardErrorResponsesOperationFilter : IOperationFilter {
    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        AddApiErrorResponse(operation, context, StatusCodes.Status500InternalServerError);

        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor controllerAction) {
            return;
        }

        object[] actionAttributes = controllerAction.MethodInfo.GetCustomAttributes(inherit: true);
        if (actionAttributes.OfType<AllowAnonymousAttribute>().Any()) {
            return;
        }

        IAuthorizeData[] authorizeAttributes =
        [
            .. controllerAction.MethodInfo
                        .GetCustomAttributes(inherit: true)
                        .OfType<IAuthorizeData>()
,
            .. controllerAction.ControllerTypeInfo.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>(),
        ];

        if (authorizeAttributes.Length == 0) {
            return;
        }

        AddApiErrorResponse(operation, context, StatusCodes.Status401Unauthorized);

        if (authorizeAttributes.Any(static attribute =>
                !string.IsNullOrWhiteSpace(attribute.Roles) ||
                !string.IsNullOrWhiteSpace(attribute.Policy))) {
            AddApiErrorResponse(operation, context, StatusCodes.Status403Forbidden);
        }
    }

    private static void AddApiErrorResponse(OpenApiOperation operation, OperationFilterContext context, int statusCode) {
        operation.Responses ??= [];

        string statusCodeText = statusCode.ToString(CultureInfo.InvariantCulture);
        if (operation.Responses.ContainsKey(statusCodeText)) {
            return;
        }

        operation.Responses[statusCodeText] = new OpenApiResponse {
            Description = GetDescription(statusCode),
            Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal) {
                ["application/json"] = new() {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorHttpResponse), context.SchemaRepository),
                },
            },
        };
    }

    private static string GetDescription(int statusCode) =>
        statusCode switch {
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            _ => "Error",
        };
}

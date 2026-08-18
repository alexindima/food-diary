using System.Globalization;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodDiary.Web.Api.Swagger;

public sealed class StandardErrorResponsesOperationFilter : IOperationFilter {
    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        AddApiErrorResponse(operation, context, StatusCodes.Status500InternalServerError);
        if (operation.RequestBody is not null) {
            AddApiErrorResponse(operation, context, StatusCodes.Status400BadRequest);
            AddApiErrorResponse(operation, context, StatusCodes.Status413PayloadTooLarge);
        }

        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor controllerAction) {
            return;
        }

        object[] actionAttributes = controllerAction.MethodInfo.GetCustomAttributes(inherit: true);
        object[] controllerAttributes = controllerAction.ControllerTypeInfo.GetCustomAttributes(inherit: true);
        if (IsRateLimited(actionAttributes, controllerAttributes)) {
            AddApiErrorResponse(operation, context, StatusCodes.Status429TooManyRequests);
        }

        if (actionAttributes.OfType<AllowAnonymousAttribute>().Any() ||
            controllerAttributes.OfType<AllowAnonymousAttribute>().Any()) {
            operation.Security = [];
            return;
        }

        IAuthorizeData[] authorizeAttributes =
        [
            .. controllerAction.MethodInfo.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>(),
            .. controllerAttributes.OfType<IAuthorizeData>(),
        ];

        if (authorizeAttributes.Length == 0) {
            operation.Security = [];
            return;
        }

        operation.Security = [new OpenApiSecurityRequirement {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document, externalResource: null)] = [],
        }];

        AddApiErrorResponse(operation, context, StatusCodes.Status401Unauthorized);

        bool blocksImpersonatedAccess =
            actionAttributes.OfType<BlockImpersonatedAccessAttribute>().Any() ||
            controllerAttributes.OfType<BlockImpersonatedAccessAttribute>().Any() ||
            (IsUnsafeMethod(context.ApiDescription.HttpMethod) &&
             !actionAttributes.OfType<AllowImpersonatedAccessAttribute>().Any());
        if (blocksImpersonatedAccess ||
            authorizeAttributes.Any(static attribute =>
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
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status413PayloadTooLarge => "Payload Too Large",
            StatusCodes.Status429TooManyRequests => "Too Many Requests",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            _ => "Error",
        };

    private static bool IsRateLimited(object[] actionAttributes, object[] controllerAttributes) {
        if (actionAttributes.OfType<DisableRateLimitingAttribute>().Any()) {
            return false;
        }

        return actionAttributes.OfType<EnableRateLimitingAttribute>().Any() ||
               controllerAttributes.OfType<EnableRateLimitingAttribute>().Any();
    }

    private static bool IsUnsafeMethod(string? method) =>
        method is not null &&
        !HttpMethods.IsGet(method) &&
        !HttpMethods.IsHead(method) &&
        !HttpMethods.IsOptions(method) &&
        !HttpMethods.IsTrace(method);
}

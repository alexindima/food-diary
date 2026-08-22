using System.Globalization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodDiary.Web.Api.Swagger;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class PresentationTransportOperationFilter : IOperationFilter {
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor controllerAction) {
            return;
        }

        RemoveCurrentUserParameters(operation, controllerAction);

        EnableIdempotencyAttribute? idempotency = controllerAction.MethodInfo
            .GetCustomAttributes(inherit: true)
            .OfType<EnableIdempotencyAttribute>()
            .SingleOrDefault();
        if (idempotency is null) {
            return;
        }

        operation.Parameters ??= [];
        if (!operation.Parameters.Any(static parameter =>
                string.Equals(parameter.Name, IdempotencyKeyHeader, StringComparison.OrdinalIgnoreCase))) {
            operation.Parameters.Add(new OpenApiParameter {
                Name = IdempotencyKeyHeader,
                In = ParameterLocation.Header,
                Required = idempotency.RequireKey,
                Description = "Deduplicates retries of this POST request for 24 hours.",
                Schema = new OpenApiSchema {
                    Type = JsonSchemaType.String,
                    MinLength = 1,
                    MaxLength = 128,
                    Pattern = "^[A-Za-z0-9._:-]+$",
                },
            });
        }

        AddApiErrorResponse(operation, context, StatusCodes.Status400BadRequest);
        AddApiErrorResponse(operation, context, StatusCodes.Status409Conflict);
        AddApiErrorResponse(operation, context, StatusCodes.Status503ServiceUnavailable);
    }

    private static void RemoveCurrentUserParameters(
        OpenApiOperation operation,
        ControllerActionDescriptor controllerAction) {
        if (operation.Parameters is null) {
            return;
        }

        HashSet<string> currentUserParameters = [.. controllerAction.MethodInfo
            .GetParameters()
            .Where(static parameter => parameter.GetCustomAttributes(typeof(FromCurrentUserAttribute), inherit: true).Length > 0)
            .Select(static parameter => parameter.Name)
            .OfType<string>()];

        operation.Parameters = [.. operation.Parameters.Where(parameter =>
            parameter.Name is null || !currentUserParameters.Contains(parameter.Name))];
    }

    private static void AddApiErrorResponse(
        OpenApiOperation operation,
        OperationFilterContext context,
        int statusCode) {
        operation.Responses ??= [];
        string statusCodeText = statusCode.ToString(CultureInfo.InvariantCulture);
        if (operation.Responses.ContainsKey(statusCodeText)) {
            return;
        }

        operation.Responses[statusCodeText] = new OpenApiResponse {
            Description = statusCode switch {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
                _ => "Error",
            },
            Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal) {
                ["application/json"] = new() {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorHttpResponse), context.SchemaRepository),
                },
            },
        };
    }
}

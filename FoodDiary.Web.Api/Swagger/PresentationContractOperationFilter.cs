using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodDiary.Web.Api.Swagger;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class PresentationContractOperationFilter : IOperationFilter {
    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor controllerAction) {
            return;
        }

        ApplyQueryContract(operation, controllerAction.MethodInfo);
        ApplyFileResponseContract(operation, controllerAction.MethodInfo);
    }

    private static void ApplyQueryContract(OpenApiOperation operation, MethodInfo actionMethod) {
        if (operation.Parameters is null) {
            return;
        }

        foreach (ParameterInfo actionParameter in actionMethod.GetParameters()) {
            if (!actionParameter.ParameterType.Name.EndsWith("HttpQuery", StringComparison.Ordinal)) {
                ApplyQueryParameterContract(operation, actionParameter);
                continue;
            }

            ConstructorInfo constructor = actionParameter.ParameterType
                .GetConstructors()
                .OrderByDescending(static candidate => candidate.GetParameters().Length)
                .First();

            foreach (ParameterInfo queryParameter in constructor.GetParameters()) {
                ApplyQueryParameterContract(operation, queryParameter);
            }
        }
    }

    private static void ApplyQueryParameterContract(OpenApiOperation operation, ParameterInfo parameter) {
        var openApiParameter = operation.Parameters?.FirstOrDefault(candidate =>
            candidate.In == ParameterLocation.Query &&
            string.Equals(candidate.Name, parameter.Name, StringComparison.OrdinalIgnoreCase)) as OpenApiParameter;
        if (openApiParameter?.Schema is not OpenApiSchema schema) {
            return;
        }

        if (parameter.GetCustomAttribute<MaxLengthAttribute>() is { } maxLength) {
            schema.MaxLength = maxLength.Length;
        }

        if (parameter.GetCustomAttribute<AllowedQueryValuesAttribute>() is { } allowedValues) {
            schema.Enum = [.. allowedValues.Values.Select(static value => JsonValue.Create(value))];
        }

        if (parameter.GetCustomAttribute<OpenApiNumericRangeAttribute>() is { } range) {
            schema.Minimum = range.Minimum.ToString("R", CultureInfo.InvariantCulture);
            schema.Maximum = range.Maximum?.ToString("R", CultureInfo.InvariantCulture);
        }

        if (parameter.HasDefaultValue && parameter.DefaultValue is not null and not DBNull) {
            schema.Default = JsonSerializer.SerializeToNode(parameter.DefaultValue);
        }
    }

    private static void ApplyFileResponseContract(OpenApiOperation operation, MethodInfo actionMethod) {
        ProducesFileResponseAttribute? fileResponse = actionMethod.GetCustomAttribute<ProducesFileResponseAttribute>();
        if (fileResponse is null || operation.Responses is null ||
            !operation.Responses.TryGetValue(StatusCodes.Status200OK.ToString(CultureInfo.InvariantCulture), out IOpenApiResponse? response) ||
            response is not OpenApiResponse concreteResponse) {
            return;
        }

        concreteResponse.Content = fileResponse.ContentTypes.ToDictionary(
            static contentType => contentType,
            static _ => new OpenApiMediaType {
                Schema = new OpenApiSchema {
                    Type = JsonSchemaType.String,
                    Format = "binary",
                },
            },
            StringComparer.Ordinal);
        concreteResponse.Headers ??= new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal);
        concreteResponse.Headers["Content-Disposition"] = new OpenApiHeader {
            Description = "Attachment filename for the exported file.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        };
    }
}

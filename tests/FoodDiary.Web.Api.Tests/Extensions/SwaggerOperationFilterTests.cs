using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Web.Api.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class SwaggerOperationFilterTests {
    [Fact]
    public void Apply_ForAnonymousAction_AddsOnlyStandard500Response() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.Anonymous)));

        Assert.True(operation.Responses.ContainsKey("500"));
        Assert.False(operation.Responses.ContainsKey("401"));
        Assert.False(operation.Responses.ContainsKey("403"));
        Assert.NotNull(operation.Security);
        Assert.Empty(operation.Security!);
    }

    [Fact]
    public void Apply_ForAuthorizedAction_Adds401And500Responses() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.Authorized)));

        Assert.True(operation.Responses.ContainsKey("500"));
        Assert.True(operation.Responses.ContainsKey("401"));
        Assert.False(operation.Responses.ContainsKey("403"));
        Assert.Equal("Unauthorized", operation.Responses["401"].Description);
        Assert.NotNull(operation.Responses["401"].Content);
        Assert.True(operation.Responses["401"].Content!.ContainsKey("application/json"));
        OpenApiSecurityRequirement requirement = Assert.Single(operation.Security!);
        Assert.IsType<OpenApiSecuritySchemeReference>(Assert.Single(requirement.Keys));
    }

    [Fact]
    public void Apply_ForAuthorizedUnsafeAction_AddsImpersonationForbiddenResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.Authorized), HttpMethods.Post));

        Assert.True(operation.Responses.ContainsKey("403"));
    }

    [Fact]
    public void Apply_ForExplicitlyAllowedImpersonatedUnsafeAction_DoesNotAddImpersonationForbiddenResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.ImpersonationAllowed), HttpMethods.Post));

        Assert.False(operation.Responses.ContainsKey("403"));
    }

    [Fact]
    public void Apply_ForExplicitlyBlockedSafeAction_AddsImpersonationForbiddenResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.ImpersonationBlocked), HttpMethods.Get));

        Assert.True(operation.Responses.ContainsKey("403"));
    }

    [Theory]
    [InlineData(nameof(TestController.Anonymous))]
    [InlineData(nameof(TestController.Authorized))]
    public void Apply_ForRequestBody_AddsBadRequestAndPayloadTooLargeResponses(string methodName) {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation {
            RequestBody = new OpenApiRequestBody(),
            Responses = [],
        };

        filter.Apply(operation, CreateContext(methodName, HttpMethods.Post));

        Assert.Multiple(
            () => Assert.True(operation.Responses.ContainsKey("400")),
            () => Assert.Equal("Bad Request", operation.Responses["400"].Description),
            () => Assert.True(operation.Responses["400"].Content!.ContainsKey("application/json")),
            () => Assert.True(operation.Responses.ContainsKey("413")),
            () => Assert.Equal("Payload Too Large", operation.Responses["413"].Description),
            () => Assert.True(operation.Responses["413"].Content!.ContainsKey("application/json")));
    }

    [Fact]
    public void Apply_ForRateLimitedAction_AddsTooManyRequestsResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.RateLimited)));

        Assert.Multiple(
            () => Assert.True(operation.Responses.ContainsKey("429")),
            () => Assert.Equal("Too Many Requests", operation.Responses["429"].Description),
            () => Assert.True(operation.Responses["429"].Content!.ContainsKey("application/json")));
    }

    [Fact]
    public void Apply_ForControllerRateLimit_AddsTooManyRequestsResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(typeof(RateLimitedController), nameof(RateLimitedController.Inherited)));

        Assert.True(operation.Responses.ContainsKey("429"));
    }

    [Fact]
    public void Apply_ForDisabledControllerRateLimit_DoesNotAddTooManyRequestsResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(typeof(RateLimitedController), nameof(RateLimitedController.Disabled)));

        Assert.False(operation.Responses.ContainsKey("429"));
    }

    [Fact]
    public void Apply_ForRoleAuthorizedAction_AddsForbiddenResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.AdminOnly)));

        Assert.True(operation.Responses.ContainsKey("401"));
        Assert.True(operation.Responses.ContainsKey("403"));
        Assert.Equal("Forbidden", operation.Responses["403"].Description);
    }

    [Fact]
    public void Apply_ForPolicyAuthorizedAction_AddsForbiddenResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.PolicyOnly)));

        Assert.True(operation.Responses.ContainsKey("401"));
        Assert.True(operation.Responses.ContainsKey("403"));
    }

    [Fact]
    public void Apply_ForNonControllerAction_AddsOnlyStandard500Response() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };
        OperationFilterContext context = CreateContext(new ActionDescriptor());

        filter.Apply(operation, context);

        Assert.True(operation.Responses.ContainsKey("500"));
        Assert.False(operation.Responses.ContainsKey("401"));
        Assert.False(operation.Responses.ContainsKey("403"));
        Assert.Null(operation.Security);
    }

    [Fact]
    public void Apply_ForActionWithoutAuthenticationMetadata_DeclaresNoSecurity() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.Unclassified)));

        Assert.NotNull(operation.Security);
        Assert.Empty(operation.Security!);
        Assert.False(operation.Responses.ContainsKey("401"));
    }

    [Fact]
    public void Apply_DoesNotOverwriteExistingResponse() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation {
            Responses = new OpenApiResponses {
                ["500"] = new OpenApiResponse { Description = "Custom" },
            },
        };

        filter.Apply(operation, CreateContext(nameof(TestController.Authorized)));

        Assert.Equal("Custom", operation.Responses["500"].Description);
    }

    [Fact]
    public void GetDescription_ForUnknownStatusCode_ReturnsGenericErrorDescription() {
        MethodInfo? method = typeof(StandardErrorResponsesOperationFilter).GetMethod(
            "GetDescription",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        object? description = method!.Invoke(null, [StatusCodes.Status418ImATeapot]);

        Assert.Equal("Error", description);
    }

    [Fact]
    public void PresentationTransportFilter_RemovesCurrentUserParameter() {
        var filter = new PresentationTransportOperationFilter();
        var operation = new OpenApiOperation {
            Parameters = [
                new OpenApiParameter { Name = "userId", In = ParameterLocation.Query },
                new OpenApiParameter { Name = "page", In = ParameterLocation.Query },
            ],
        };

        filter.Apply(operation, CreateContext(nameof(TestController.CurrentUserBound)));

        OpenApiParameter parameter = Assert.IsType<OpenApiParameter>(Assert.Single(operation.Parameters!));
        Assert.Equal("page", parameter.Name);
    }

    [Theory]
    [InlineData(nameof(TestController.OptionalIdempotency), false)]
    [InlineData(nameof(TestController.RequiredIdempotency), true)]
    public void PresentationTransportFilter_DocumentsIdempotencyContract(string methodName, bool required) {
        var filter = new PresentationTransportOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(methodName));

        OpenApiParameter parameter = Assert.IsType<OpenApiParameter>(Assert.Single(operation.Parameters!));
        OpenApiSchema schema = Assert.IsType<OpenApiSchema>(parameter.Schema);
        Assert.Multiple(
            () => Assert.Equal("Idempotency-Key", parameter.Name),
            () => Assert.Equal(ParameterLocation.Header, parameter.In),
            () => Assert.Equal(required, parameter.Required),
            () => Assert.Equal(1, schema.MinLength),
            () => Assert.Equal(128, schema.MaxLength),
            () => Assert.True(operation.Responses.ContainsKey("400")),
            () => Assert.True(operation.Responses.ContainsKey("409")));
    }

    [Fact]
    public void PresentationContractFilter_DocumentsGroupedQueryValidation() {
        var filter = new PresentationContractOperationFilter();
        var operation = new OpenApiOperation {
            Parameters = [
                new OpenApiParameter {
                    Name = "Filter",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                },
                new OpenApiParameter {
                    Name = "Page",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.Integer },
                },
            ],
        };

        filter.Apply(operation, CreateContext(nameof(TestController.GroupedQuery)));

        IOpenApiSchema schema = operation.Parameters!.Single(parameter => string.Equals(parameter.Name, "Filter", StringComparison.Ordinal)).Schema!;
        IOpenApiSchema pageSchema = operation.Parameters.Single(parameter => string.Equals(parameter.Name, "Page", StringComparison.Ordinal)).Schema!;
        string[] allowedValues = [.. schema.Enum!.Select(static value => value!.GetValue<string>())];
        Assert.Multiple(
            () => Assert.Equal(12, schema.MaxLength),
            () => Assert.Equal(["one", "two"], allowedValues),
            () => Assert.Equal("1", pageSchema.Minimum),
            () => Assert.Equal("100", pageSchema.Maximum),
            () => Assert.Equal(2, pageSchema.Default!.GetValue<int>()));
    }

    [Fact]
    public void PresentationContractFilter_DocumentsDirectQueryRangeAndDefault() {
        var filter = new PresentationContractOperationFilter();
        var operation = new OpenApiOperation {
            Parameters = [
                new OpenApiParameter {
                    Name = "limit",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.Integer },
                },
            ],
        };

        filter.Apply(operation, CreateContext(nameof(TestController.DirectQuery)));

        IOpenApiSchema schema = Assert.Single(operation.Parameters!).Schema!;
        Assert.Multiple(
            () => Assert.Equal("1", schema.Minimum),
            () => Assert.Equal("50", schema.Maximum),
            () => Assert.Equal(20, schema.Default!.GetValue<int>()));
    }

    [Fact]
    public void PresentationContractFilter_DocumentsMinimumOnlyAndIgnoresUnmatchedParameter() {
        var filter = new PresentationContractOperationFilter();
        var operation = new OpenApiOperation {
            Parameters = [
                new OpenApiParameter {
                    Name = "unrelated",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                },
                new OpenApiParameter {
                    Name = "minimumOnly",
                    In = ParameterLocation.Query,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.Number },
                },
            ],
        };

        filter.Apply(operation, CreateContext(nameof(TestController.MinimumOnlyQuery)));

        IOpenApiSchema schema = operation.Parameters.Single(parameter =>
            string.Equals(parameter.Name, "minimumOnly", StringComparison.Ordinal)).Schema!;
        Assert.Multiple(
            () => Assert.Equal("0", schema.Minimum),
            () => Assert.Null(schema.Maximum),
            () => Assert.Null(operation.Parameters.Single(parameter =>
                string.Equals(parameter.Name, "unrelated", StringComparison.Ordinal)).Schema!.Default));
    }

    [Fact]
    public void PresentationContractFilter_DocumentsFileResponseMediaAndHeader() {
        var filter = new PresentationContractOperationFilter();
        var operation = new OpenApiOperation {
            Responses = new OpenApiResponses {
                ["200"] = new OpenApiResponse { Description = "OK" },
            },
        };

        filter.Apply(operation, CreateContext(nameof(TestController.FileResponse)));

        IOpenApiResponse response = operation.Responses!["200"];
        Assert.Multiple(
            () => Assert.Equal(["application/pdf", "text/csv"], response.Content!.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal),
            () => Assert.All(response.Content!.Values, mediaType => {
                Assert.Equal(JsonSchemaType.String, mediaType.Schema!.Type);
                Assert.Equal("binary", mediaType.Schema.Format);
            }),
            () => Assert.True(response.Headers!.ContainsKey("Content-Disposition")));
    }

    [Fact]
    public void PresentationContractFilter_PreservesExistingFileResponseHeaders() {
        var filter = new PresentationContractOperationFilter();
        var operation = new OpenApiOperation {
            Responses = new OpenApiResponses {
                ["200"] = new OpenApiResponse {
                    Description = "OK",
                    Headers = new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal) {
                        ["X-Export-Version"] = new OpenApiHeader(),
                    },
                },
            },
        };

        filter.Apply(operation, CreateContext(nameof(TestController.FileResponse)));

        Assert.Equal(
            ["Content-Disposition", "X-Export-Version"],
            operation.Responses!["200"].Headers!.Keys.Order(StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    [Fact]
    public void PresentationContractFilter_IgnoresUnsupportedDescriptionsAndMissingMetadata() {
        var filter = new PresentationContractOperationFilter();
        var operation = new OpenApiOperation();

        filter.Apply(operation, CreateContext(new ActionDescriptor()));
        filter.Apply(operation, CreateContext(nameof(TestController.GroupedQuery)));

        Assert.Null(operation.Parameters);
    }

    private static OperationFilterContext CreateContext(string methodName, string? httpMethod = null) {
        return CreateContext(typeof(TestController), methodName, httpMethod);
    }

    private static OperationFilterContext CreateContext(Type controllerType, string methodName, string? httpMethod = null) {
        MethodInfo methodInfo = controllerType.GetMethod(methodName)!;
        var actionDescriptor = new ControllerActionDescriptor {
            MethodInfo = methodInfo,
            ControllerTypeInfo = System.Reflection.IntrospectionExtensions.GetTypeInfo(controllerType),
        };

        return CreateContext(actionDescriptor, methodInfo, httpMethod);
    }

    private static OperationFilterContext CreateContext(ActionDescriptor actionDescriptor) {
        MethodInfo methodInfo = typeof(TestController).GetMethod(nameof(TestController.Authorized))!;
        return CreateContext(actionDescriptor, methodInfo, httpMethod: null);
    }

    private static OperationFilterContext CreateContext(
        ActionDescriptor actionDescriptor,
        System.Reflection.MethodInfo methodInfo,
        string? httpMethod) {
        var apiDescription = new ApiDescription {
            HttpMethod = httpMethod,
            ActionDescriptor = actionDescriptor,
        };
        var schemaGenerator = new SchemaGenerator(
            new SchemaGeneratorOptions(),
            new JsonSerializerDataContractResolver(new System.Text.Json.JsonSerializerOptions()));

        return new OperationFilterContext(
            apiDescription,
            schemaGenerator,
            new SchemaRepository(),
            new OpenApiDocument(),
            methodInfo);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestController : ControllerBase {
        [AllowAnonymous]
        public OkResult Anonymous() => Ok();

        [Authorize]
        public OkResult Authorized() => Ok();

        [Authorize]
        [AllowImpersonatedAccess]
        public OkResult ImpersonationAllowed() => Ok();

        [Authorize]
        [BlockImpersonatedAccess]
        public OkResult ImpersonationBlocked() => Ok();

        [Authorize(Roles = "Admin")]
        public OkResult AdminOnly() => Ok();

        [Authorize(Policy = "OwnerOnly")]
        public OkResult PolicyOnly() => Ok();

        public OkResult Unclassified() => Ok();

        public IActionResult CurrentUserBound([FromCurrentUser] Guid userId, int page) => Ok(new { userId, page });

        [EnableIdempotency]
        public OkResult OptionalIdempotency() => Ok();

        [EnableIdempotency(requireKey: true)]
        public OkResult RequiredIdempotency() => Ok();

        [EnableRateLimiting("test")]
        public OkResult RateLimited() => Ok();

        public IActionResult GroupedQuery([FromQuery] TestHttpQuery query) => Ok(query);

        public IActionResult DirectQuery(
            [FromQuery, OpenApiNumericRange(1, 50)] int limit = 20) => Ok(limit);

        public IActionResult MinimumOnlyQuery(
            [FromQuery, OpenApiNumericRange(0)] double? minimumOnly = null) => Ok(minimumOnly);

        [ProducesFileResponse("text/csv", "application/pdf")]
        public OkResult FileResponse() => Ok();

    }

    [ExcludeFromCodeCoverage]
    private sealed record TestHttpQuery(
        [MaxLength(12)]
        [AllowedQueryValues("one", "two")] string? Filter,
        [OpenApiNumericRange(1, 100)] int Page = 2);

    [EnableRateLimiting("test")]
    [ExcludeFromCodeCoverage]
    private sealed class RateLimitedController : ControllerBase {
        public OkResult Inherited() => Ok();

        [DisableRateLimiting]
        public OkResult Disabled() => Ok();
    }
}

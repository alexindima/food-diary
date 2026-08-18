using System.Reflection;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Web.Api.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
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

    private static OperationFilterContext CreateContext(string methodName) {
        MethodInfo methodInfo = typeof(TestController).GetMethod(methodName)!;
        var apiDescription = new ApiDescription {
            ActionDescriptor = new ControllerActionDescriptor {
                MethodInfo = methodInfo,
                ControllerTypeInfo = System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(TestController)),
            },
        };

        return CreateContext(apiDescription.ActionDescriptor, methodInfo);
    }

    private static OperationFilterContext CreateContext(ActionDescriptor actionDescriptor) {
        MethodInfo methodInfo = typeof(TestController).GetMethod(nameof(TestController.Authorized))!;
        return CreateContext(actionDescriptor, methodInfo);
    }

    private static OperationFilterContext CreateContext(ActionDescriptor actionDescriptor, System.Reflection.MethodInfo methodInfo) {
        var apiDescription = new ApiDescription {
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
    }
}

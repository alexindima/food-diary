using FoodDiary.Web.Api.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class SwaggerOperationFilterTests {
    [Fact]
    public void Apply_ForAnonymousAction_AddsOnlyStandard500Response() {
        var filter = new StandardErrorResponsesOperationFilter();
        var operation = new OpenApiOperation { Responses = [] };

        filter.Apply(operation, CreateContext(nameof(TestController.Anonymous)));

        Assert.True(operation.Responses.ContainsKey("500"));
        Assert.False(operation.Responses.ContainsKey("401"));
        Assert.False(operation.Responses.ContainsKey("403"));
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

    private static OperationFilterContext CreateContext(string methodName) {
        var methodInfo = typeof(TestController).GetMethod(methodName)!;
        var apiDescription = new ApiDescription {
            ActionDescriptor = new ControllerActionDescriptor {
                MethodInfo = methodInfo,
                ControllerTypeInfo = System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(TestController)),
            },
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

    private sealed class TestController : ControllerBase {
        [AllowAnonymous]
        public OkResult Anonymous() => Ok();

        [Authorize]
        public OkResult Authorized() => Ok();

        [Authorize(Roles = "Admin")]
        public OkResult AdminOnly() => Ok();
    }
}

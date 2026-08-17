using System.Reflection;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Ai;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Features.Cycles;
using FoodDiary.Presentation.Api.Features.Export;
using FoodDiary.Presentation.Api.Features.Fasting;
using FoodDiary.Presentation.Api.Features.Images;
using FoodDiary.Presentation.Api.Features.Meals;
using FoodDiary.Presentation.Api.Features.Products;
using FoodDiary.Presentation.Api.Features.Recipes;
using FoodDiary.Presentation.Api.Features.Statistics;
using FoodDiary.Presentation.Api.Features.Users;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ThinControllerFailureContractTests {
    private static readonly Error ValidationFailure = new("Validation.Invalid", "The request is invalid.");

    public static TheoryData<Type> Controllers => [
        typeof(AdminDashboardController),
        typeof(AiUsageController),
        typeof(AuthPasswordController),
        typeof(CyclesController),
        typeof(ExportController),
        typeof(FastingReadController),
        typeof(ImagesController),
        typeof(MealsController),
        typeof(MenstrualEpisodesController),
        typeof(ProductsController),
        typeof(RecipesController),
        typeof(StatisticsController),
        typeof(UserOverviewController),
        typeof(WaistGoalsController),
        typeof(WeightGoalsController),
    ];

    [Theory]
    [MemberData(nameof(Controllers))]
    public async Task Actions_MapTransportInputsAndNormalizeApplicationFailures(Type controllerType) {
        FailureSender sender = new();
        ControllerBase controller = Assert.IsAssignableFrom<ControllerBase>(Activator.CreateInstance(controllerType, sender));
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        MethodInfo[] actions = [.. controllerType.GetMethods()
            .Where(static method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: false).Any())];

        Assert.NotEmpty(actions);
        foreach (MethodInfo action in actions) {
            object?[] arguments = [.. action.GetParameters().Select(CreateArgument)];

            object? invocation = action.Invoke(controller, arguments);
            Task<IActionResult> actionTask = Assert.IsAssignableFrom<Task<IActionResult>>(invocation);
            IActionResult result = await actionTask;

            ObjectResult objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
            ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
            Assert.Multiple(
                () => Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode),
                () => Assert.Equal(ValidationFailure.Code, response.Error));
        }
    }

    private static object? CreateArgument(ParameterInfo parameter) {
        if (parameter.HasDefaultValue) {
            return parameter.DefaultValue;
        }

        return CreateValue(parameter.ParameterType);
    }

    private static object? CreateValue(Type type) {
        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType is not null) {
            return null;
        }

        if (type == typeof(string)) {
            return "value";
        }

        if (type == typeof(Guid)) {
            return Guid.NewGuid();
        }

        if (type == typeof(DateTime)) {
            return DateTime.UtcNow;
        }

        if (type == typeof(DateOnly)) {
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }

        if (type.IsEnum || type.IsValueType) {
            return Activator.CreateInstance(type);
        }

        if (TryCreateEmptyCollection(type, out object? collection)) {
            return collection;
        }

        ConstructorInfo constructor = type.GetConstructors()
            .OrderByDescending(static candidate => candidate.GetParameters().Length)
            .First();
        object?[] arguments = [.. constructor.GetParameters().Select(CreateArgument)];
        return constructor.Invoke(arguments);
    }

    private static bool TryCreateEmptyCollection(Type type, out object? collection) {
        if (!type.IsGenericType) {
            collection = null;
            return false;
        }

        Type genericType = type.GetGenericTypeDefinition();
        Type[] supportedTypes = [
            typeof(IEnumerable<>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>),
            typeof(ICollection<>),
            typeof(IList<>),
            typeof(List<>),
        ];
        if (!supportedTypes.Contains(genericType)) {
            collection = null;
            return false;
        }

        collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]));
        return true;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailureSender : ISender {
        private static readonly MethodInfo GenericFailureFactory = typeof(Result).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method => string.Equals(method.Name, nameof(Result.Failure), StringComparison.Ordinal) && method.IsGenericMethodDefinition);

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default) {
            if (typeof(TResponse) == typeof(Result)) {
                return Task.FromResult((TResponse)(object)Result.Failure(ValidationFailure));
            }

            Type valueType = typeof(TResponse).GetGenericArguments().Single();
            object failure = GenericFailureFactory.MakeGenericMethod(valueType).Invoke(null, [ValidationFailure])!;
            return Task.FromResult((TResponse)failure);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

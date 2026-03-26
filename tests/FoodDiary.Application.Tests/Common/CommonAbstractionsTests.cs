using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Utilities;
using System.Reflection;

namespace FoodDiary.Application.Tests.Common;

public class CommonAbstractionsTests {
    [Fact]
    public void ApplicationLayer_UsesCentralErrorCatalog_ExceptValidationBehavior() {
        var applicationRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "FoodDiary.Application"));
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Path.Combine(applicationRoot, "Common", "Abstractions", "Result", "Errors.cs"),
            Path.Combine(applicationRoot, "Common", "Behaviors", "ValidationBehavior.cs"),
        };

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(path => allowedFiles.Contains(path) is false)
            .Where(ContainsAdHocErrorConstruction)
            .Select(path => Path.GetRelativePath(applicationRoot, path))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void CentralErrorCatalog_DefinesErrorKind_ForAllPublishedErrors() {
        var missingKinds = typeof(Errors)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetErrorsFromType)
            .Where(static error => error.Kind is null)
            .Select(static error => error.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missingKinds);
    }

    [Fact]
    public void ResultFailure_WithGenericType_ThrowsOnValueAccess() {
        var result = Result.Failure<string>(Errors.Validation.Required("name"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public async Task ValidationBehavior_ForGenericResult_UsesDefaultValidationCode_WhenErrorCodeIsEmpty() {
        var validator = new GenericCommandValidator();
        var behavior = new ValidationBehavior<GenericCommand, Result<string>>([validator]);
        var command = new GenericCommand("");

        var result = await behavior.Handle(command, _ => Task.FromResult(Result.Success("ok")), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ValidationBehavior_ForNonGenericResult_ReturnsFailureResult() {
        var validator = new NonGenericCommandValidator();
        var behavior = new ValidationBehavior<NonGenericCommand, Result>([validator]);
        var command = new NonGenericCommand("");

        var result = await behavior.Handle(command, _ => Task.FromResult(Result.Success()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public void SecurityTokenGenerator_WithInvalidLength_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => SecurityTokenGenerator.GenerateUrlSafeToken(0));
    }

    [Fact]
    public void SecurityTokenGenerator_ReturnsUrlSafeToken() {
        var token = SecurityTokenGenerator.GenerateUrlSafeToken(32);

        Assert.NotEmpty(token);
        Assert.DoesNotContain("+", token, StringComparison.Ordinal);
        Assert.DoesNotContain("/", token, StringComparison.Ordinal);
        Assert.DoesNotContain("=", token, StringComparison.Ordinal);
    }

    private sealed record GenericCommand(string Value) : ICommand<Result<string>>;

    private sealed class GenericCommandValidator : AbstractValidator<GenericCommand> {
        public GenericCommandValidator() {
            RuleFor(x => x.Value)
                .Custom((_, context) => context.AddFailure(new ValidationFailure(
                    nameof(GenericCommand.Value),
                    "value is required") {
                    ErrorCode = " "
                }));
        }
    }

    private sealed record NonGenericCommand(string Value) : ICommand<Result>;

    private sealed class NonGenericCommandValidator : AbstractValidator<NonGenericCommand> {
        public NonGenericCommandValidator() {
            RuleFor(x => x.Value)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("value is required");
        }
    }

    private static bool ContainsAdHocErrorConstruction(string path) {
        var content = File.ReadAllText(path);
        return content.Contains("new Error(", StringComparison.Ordinal) ||
               content.Contains("new Error (", StringComparison.Ordinal);
    }

    private static IEnumerable<Error> GetErrorsFromType(Type type) {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static)) {
            if (property.PropertyType != typeof(Error) || property.GetIndexParameters().Length > 0) {
                continue;
            }

            if (property.GetValue(null) is Error error) {
                yield return error;
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (method.ReturnType != typeof(Error) || method.IsSpecialName) {
                continue;
            }

            var arguments = method.GetParameters()
                .Select(CreateSampleArgument)
                .ToArray();

            if (method.Invoke(null, arguments) is Error error) {
                yield return error;
            }
        }
    }

    private static object? CreateSampleArgument(ParameterInfo parameter) {
        var parameterType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        if (parameterType == typeof(Guid)) {
            return Guid.Empty;
        }

        if (parameterType == typeof(DateTime)) {
            return DateTime.UnixEpoch;
        }

        if (parameterType == typeof(string)) {
            return parameter.Name switch {
                "field" => "field",
                "reason" => "reason",
                "locale" => "en",
                _ => "sample",
            };
        }

        throw new InvalidOperationException($"Unsupported error catalog parameter type: {parameter.ParameterType.FullName}");
    }
}

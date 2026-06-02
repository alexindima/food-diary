using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Services;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Application.Tests.Common;

public class CommonAbstractionsTests {
    private static readonly TimeSpan ErrorCodeRegexTimeout = TimeSpan.FromSeconds(1);

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
    public void ApplicationLayer_StringErrorCodes_UseKnownCatalogCodes() {
        var applicationRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "FoodDiary.Application"));
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Path.Combine(applicationRoot, "Common", "Abstractions", "Result", "Errors.cs"),
            Path.Combine(applicationRoot, "Common", "Abstractions", "Result", "ErrorKindResolver.cs"),
        };

        var knownCodes = GetKnownErrorCodes();

        var violations = Directory.GetFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(static path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) is false)
            .Where(path => allowedFiles.Contains(path) is false)
            .SelectMany(path => GetReferencedStringErrorCodes(path)
                .Where(code => knownCodes.Contains(code) is false)
                .Select(code => $"{Path.GetRelativePath(applicationRoot, path)}: {code}"))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ResultFailure_WithGenericType_ThrowsOnValueAccess() {
        var result = Result.Failure<string>(Errors.Validation.Required("name"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void ResultGeneric_ImplicitValueConversion_ReturnsSuccessfulResult() {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void Error_ImplicitStringConversion_ReturnsCode() {
        var error = new Error("Custom.Code", "Custom message");

        string code = error;

        Assert.Equal("Custom.Code", code);
    }

    [Fact]
    public void ErrorKindResolver_ResolvesKnownFallbackPatterns() {
        Assert.Null(ErrorKindResolver.Resolve(null));
        Assert.Null(ErrorKindResolver.Resolve(" "));
        Assert.Equal(ErrorKind.Forbidden, ErrorKindResolver.Resolve("Authentication.AdminSsoForbidden"));
        Assert.Equal(ErrorKind.Unauthorized, ErrorKindResolver.Resolve("Authentication.Unknown"));
        Assert.Equal(ErrorKind.Validation, ErrorKindResolver.Resolve("Validation.Invalid"));
        Assert.Equal(ErrorKind.NotFound, ErrorKindResolver.Resolve("Product.NotAccessible"));
        Assert.Equal(ErrorKind.NotFound, ErrorKindResolver.Resolve("User.NotFound"));
        Assert.Equal(ErrorKind.Conflict, ErrorKindResolver.Resolve("Recipe.AlreadyExists"));
        Assert.Null(ErrorKindResolver.Resolve("Custom.Unknown"));
    }

    [Fact]
    public void Result_WithSuccessAndError_ThrowsInvalidOperationException() {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new TestResult(isSuccess: true, Errors.Validation.Required("name")));

        Assert.Contains("A successful result cannot contain an error.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Result_WithFailureAndNoError_ThrowsInvalidOperationException() {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new TestResult(isSuccess: false, Error.None));

        Assert.Contains("A failed result must contain an error.", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductRepository_GetByIdForUpdateAsync_DelegatesToGetByIdAsync() {
        var stub = new RecordingProductRepository();
        IProductRepository repository = stub;
        var productId = ProductId.New();
        var userId = UserId.New();
        using var cancellationTokenSource = new CancellationTokenSource();

        await repository.GetByIdForUpdateAsync(productId, userId, includePublic: false, cancellationTokenSource.Token);

        Assert.Equal(productId, stub.CapturedProductId);
        Assert.Equal(userId, stub.CapturedUserId);
        Assert.False(stub.CapturedIncludePublic);
        Assert.Equal(cancellationTokenSource.Token, stub.CapturedCancellationToken);
    }

    [Theory]
    [InlineData(UserAccountStatusFilter.All, true)]
    [InlineData(UserAccountStatusFilter.Active, false)]
    [InlineData(UserAccountStatusFilter.Inactive, false)]
    [InlineData(UserAccountStatusFilter.Deleted, true)]
    public async Task UserRepository_GetPagedAsync_WithStatus_DelegatesToLegacyIncludeDeletedOverload(
        UserAccountStatusFilter status,
        bool expectedIncludeDeleted) {
        var stub = new RecordingUserRepository();
        IUserRepository repository = stub;
        using var cancellationTokenSource = new CancellationTokenSource();

        await repository.GetPagedAsync("search", page: 2, limit: 10, status, cancellationTokenSource.Token);

        Assert.Equal("search", stub.CapturedSearch);
        Assert.Equal(2, stub.CapturedPage);
        Assert.Equal(10, stub.CapturedLimit);
        Assert.Equal(expectedIncludeDeleted, stub.CapturedIncludeDeleted);
        Assert.Equal(cancellationTokenSource.Token, stub.CapturedPagedCancellationToken);
    }

    [Fact]
    public async Task UserRepository_UpdateAsync_WithAuditEvents_DelegatesToLegacyUpdateOverload() {
        var stub = new RecordingUserRepository();
        IUserRepository repository = stub;
        var user = User.Create("user@test.com", "hashed");
        using var cancellationTokenSource = new CancellationTokenSource();

        await repository.UpdateAsync(user, [], cancellationTokenSource.Token);

        Assert.Same(user, stub.CapturedUpdatedUser);
        Assert.Equal(cancellationTokenSource.Token, stub.CapturedUpdateCancellationToken);
    }

    [Fact]
    public void JwtImpersonationContext_StoresActorAndReason() {
        var actorUserId = UserId.New();

        var context = new JwtImpersonationContext(actorUserId, "Support request");

        Assert.Equal(actorUserId, context.ActorUserId);
        Assert.Equal("Support request", context.Reason);
    }

    [Theory]
    [InlineData("/verify-email", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void EmailOptions_HasVerificationPath_ReturnsWhetherPathIsConfigured(string verificationPath, bool expected) {
        var options = new EmailOptions {
            VerificationPath = verificationPath,
        };

        Assert.Equal(expected, EmailOptions.HasVerificationPath(options));
    }

    [Theory]
    [InlineData("/reset-password", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void EmailOptions_HasPasswordResetPath_ReturnsWhetherPathIsConfigured(string passwordResetPath, bool expected) {
        var options = new EmailOptions {
            PasswordResetPath = passwordResetPath,
        };

        Assert.Equal(expected, EmailOptions.HasPasswordResetPath(options));
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

    [Fact]
    public void SystemDateTimeProvider_ReturnsUtcTime_FromTimeProviderSystem() {
        var provider = new SystemDateTimeProvider();
        var before = TimeProvider.System.GetUtcNow().UtcDateTime;
        var now = provider.UtcNow;
        var after = TimeProvider.System.GetUtcNow().UtcDateTime;

        Assert.Equal(DateTimeKind.Utc, now.Kind);
        Assert.InRange(now, before, after);
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

    private sealed class TestResult(bool isSuccess, Error error) : Result(isSuccess, error);

    private sealed class RecordingProductRepository : IProductRepository {
        public ProductId CapturedProductId { get; private set; }
        public UserId CapturedUserId { get; private set; }
        public bool CapturedIncludePublic { get; private set; }
        public CancellationToken CapturedCancellationToken { get; private set; }

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            IReadOnlyCollection<ProductType>? productTypes = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Product?> GetByIdAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            CapturedProductId = id;
            CapturedUserId = userId;
            CapturedIncludePublic = includePublic;
            CapturedCancellationToken = cancellationToken;
            return Task.FromResult<Product?>(null);
        }

        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingUserRepository : IUserRepository {
        public string? CapturedSearch { get; private set; }
        public int CapturedPage { get; private set; }
        public int CapturedLimit { get; private set; }
        public bool CapturedIncludeDeleted { get; private set; }
        public CancellationToken CapturedPagedCancellationToken { get; private set; }
        public User? CapturedUpdatedUser { get; private set; }
        public CancellationToken CapturedUpdateCancellationToken { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            string? search,
            int page,
            int limit,
            bool includeDeleted,
            CancellationToken cancellationToken = default) {
            CapturedSearch = search;
            CapturedPage = page;
            CapturedLimit = limit;
            CapturedIncludeDeleted = includeDeleted;
            CapturedPagedCancellationToken = cancellationToken;
            return Task.FromResult<(IReadOnlyList<User>, int)>(([], 0));
        }

        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
            GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) {
            CapturedUpdatedUser = user;
            CapturedUpdateCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

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

    private static HashSet<string> GetKnownErrorCodes() {
        var publishedCodes = typeof(Errors)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetErrorsFromType)
            .Select(static error => error.Code);

        var resolverCodes = typeof(ErrorKindResolver)
            .GetField("ExactMappings", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null) is IReadOnlyDictionary<string, ErrorKind> mappings
            ? mappings.Keys
            : [];

        return publishedCodes
            .Concat(resolverCodes)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetReferencedStringErrorCodes(string path) {
        var content = File.ReadAllText(path);
        var matches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"(?:WithErrorCode\(|ErrorCode\s*=\s*)""(?<code>[A-Za-z]+\.[A-Za-z]+)""",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant,
            ErrorCodeRegexTimeout);

        return matches
            .Select(match => match.Groups["code"].Value)
            .Distinct(StringComparer.Ordinal);
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

        if (parameterType == typeof(int)) {
            return 0;
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

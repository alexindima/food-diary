using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Mediator;
using System.Reflection;
using System.Reflection.Emit;

namespace FoodDiary.Application.Runtime.Tests;

[ExcludeFromCodeCoverage]
public sealed class ValidationBehaviorTests {
    [Fact]
    public async Task ValidationBehavior_ForGenericResult_UsesDefaultValidationCode_WhenErrorCodeIsEmpty() {
        var validator = new GenericCommandValidator();
        var behavior = new ValidationBehavior<GenericCommand, Result<string>>([validator]);
        var command = new GenericCommand("");

        Result<string> result = await behavior.Handle(command, _ => Task.FromResult(Result.Success("ok")), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ValidationBehavior_ForNonGenericResult_ReturnsFailureResult() {
        var validator = new NonGenericCommandValidator();
        var behavior = new ValidationBehavior<NonGenericCommand, Result>([validator]);
        var command = new NonGenericCommand("");

        Result result = await behavior.Handle(command, _ => Task.FromResult(Result.Success()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public async Task ValidationBehavior_ForUnsupportedResultSubtype_ThrowsInvalidOperationException() {
        Type unsupportedResultType = CreateUnsupportedResultSubtype();
        Type unsupportedRequestType = CreateUnsupportedRequestType(unsupportedResultType);
        Type validatorInterfaceType = typeof(IValidator<>).MakeGenericType(unsupportedRequestType);
        var validators = Array.CreateInstance(validatorInterfaceType, 1);
        validators.SetValue(
            Activator.CreateInstance(typeof(UnsupportedRequestValidator<>).MakeGenericType(unsupportedRequestType)),
            0);
        Type behaviorType = typeof(ValidationBehavior<,>).MakeGenericType(unsupportedRequestType, unsupportedResultType);
        object behavior = Activator.CreateInstance(
            behaviorType,
            [validators])!;
        object command = Activator.CreateInstance(unsupportedRequestType)!;
        object next = CreateUnsupportedResultDelegate(unsupportedResultType);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            (Task)behaviorType.GetMethod(nameof(ValidationBehavior<NonGenericCommand, Result>.Handle))!.Invoke(
                behavior,
                [command, next, CancellationToken.None])!);

        Assert.Equal("Unable to create failure result for type UnsupportedValidationResult.", ex.Message);
    }

    [ExcludeFromCodeCoverage]
    private sealed record GenericCommand(string Value) : ICommand<Result<string>>;

    [ExcludeFromCodeCoverage]
    private sealed class GenericCommandValidator : AbstractValidator<GenericCommand> {
        public GenericCommandValidator() {
            RuleFor(x => x.Value)
                .Custom((_, context) => context.AddFailure(new ValidationFailure(
                    nameof(GenericCommand.Value),
                    "value is required") {
                    ErrorCode = " ",
                }));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record NonGenericCommand(string Value) : ICommand<Result>;

    [ExcludeFromCodeCoverage]
    private sealed class NonGenericCommandValidator : AbstractValidator<NonGenericCommand> {
        public NonGenericCommandValidator() {
            RuleFor(x => x.Value)
                .NotEmpty()
                .WithErrorCode("Validation.Required")
                .WithMessage("value is required");
        }
    }

    private static Type CreateUnsupportedResultSubtype() {
        var assemblyName = new AssemblyName("FoodDiary.Application.Runtime.Tests.DynamicResults");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            "UnsupportedValidationResult",
            TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(Result));
        ConstructorInfo baseConstructor = typeof(Result).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(bool), typeof(Error)],
            modifiers: null)!;
        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            Type.EmptyTypes);
        ILGenerator il = constructorBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldsfld, typeof(Error).GetField(nameof(Error.None))!);
        il.Emit(OpCodes.Call, baseConstructor);
        il.Emit(OpCodes.Ret);

        return typeBuilder.CreateType()!;
    }

    private static Type CreateUnsupportedRequestType(Type responseType) {
        var assemblyName = new AssemblyName("FoodDiary.Application.Runtime.Tests.DynamicRequests");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            "UnsupportedValidationRequest",
            TypeAttributes.Public | TypeAttributes.Sealed);
        typeBuilder.AddInterfaceImplementation(typeof(IRequest<>).MakeGenericType(responseType));
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

        return typeBuilder.CreateType()!;
    }

    private static object CreateUnsupportedResultDelegate(Type unsupportedResultType) {
        MethodInfo method = typeof(ValidationBehaviorTests)
            .GetMethod(nameof(CreateUnsupportedResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(unsupportedResultType);

        return method.CreateDelegate(typeof(RequestHandlerDelegate<>).MakeGenericType(unsupportedResultType));
    }

    private static Task<TResponse> CreateUnsupportedResultAsync<TResponse>(CancellationToken cancellationToken)
        where TResponse : Result =>
        Task.FromResult((TResponse)Activator.CreateInstance(typeof(TResponse))!);

    [ExcludeFromCodeCoverage]
    private sealed class UnsupportedRequestValidator<TRequest> : IValidator<TRequest> {
        public ValidationResult Validate(TRequest instance) =>
            new([new ValidationFailure("Value", "value is invalid") { ErrorCode = "Validation.Invalid" }]);

        public Task<ValidationResult> ValidateAsync(TRequest instance, CancellationToken cancellation = default) =>
            Task.FromResult(new ValidationResult([new ValidationFailure("Value", "value is invalid") { ErrorCode = "Validation.Invalid" }]));

        public ValidationResult Validate(IValidationContext context) =>
            new([new ValidationFailure("Value", "value is invalid") { ErrorCode = "Validation.Invalid" }]);

        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) =>
            Task.FromResult(new ValidationResult([new ValidationFailure("Value", "value is invalid") { ErrorCode = "Validation.Invalid" }]));

        public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

        public bool CanValidateInstancesOfType(Type type) => true;
    }
}

using System.Reflection;

namespace FoodDiary.Mediator;

/// <summary>
/// Configures handler discovery and request pipeline behaviors.
/// </summary>
public sealed class MediatorServiceConfiguration {
    internal List<Assembly> Assemblies { get; } = [];

    internal List<Type> OpenBehaviors { get; } = [];

    /// <summary>
    /// Registers mediator handlers declared in the supplied assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    public void RegisterServicesFromAssembly(Assembly assembly) {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!Assemblies.Contains(assembly)) {
            Assemblies.Add(assembly);
        }
    }

    /// <summary>
    /// Adds an open generic implementation of <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
    /// </summary>
    /// <param name="behaviorType">The open generic behavior implementation type.</param>
    public void AddOpenBehavior(Type behaviorType) {
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (!behaviorType.IsGenericTypeDefinition) {
            throw new ArgumentException("Behavior type must be an open generic type definition.", nameof(behaviorType));
        }

        if (behaviorType.IsInterface || behaviorType.IsAbstract) {
            throw new ArgumentException("Behavior type must be a concrete class.", nameof(behaviorType));
        }

        Type[] genericArguments = behaviorType.GetGenericArguments();
        bool implementsPipelineBehavior = behaviorType
            .GetInterfaces()
            .Where(static interfaceType => interfaceType.IsGenericType)
            .Where(static interfaceType =>
                interfaceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .Select(static interfaceType => interfaceType.GetGenericArguments())
            .Any(interfaceArguments => interfaceArguments.SequenceEqual(genericArguments));

        if (!implementsPipelineBehavior) {
            throw new ArgumentException(
                $"Behavior type must implement {typeof(IPipelineBehavior<,>).Name} using its generic type parameters in order.",
                nameof(behaviorType));
        }

        if (!OpenBehaviors.Contains(behaviorType)) {
            OpenBehaviors.Add(behaviorType);
        }
    }
}

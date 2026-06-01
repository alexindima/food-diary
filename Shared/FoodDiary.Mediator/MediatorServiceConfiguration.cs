using System.Reflection;

namespace FoodDiary.Mediator;

public sealed class MediatorServiceConfiguration {
    internal List<Assembly> Assemblies { get; } = [];

    internal List<Type> OpenBehaviors { get; } = [];

    public void RegisterServicesFromAssembly(Assembly assembly) {
        Assemblies.Add(assembly);
    }

    public void AddOpenBehavior(Type behaviorType) {
        if (!behaviorType.IsGenericTypeDefinition) {
            throw new ArgumentException("Behavior type must be an open generic type definition.", nameof(behaviorType));
        }

        OpenBehaviors.Add(behaviorType);
    }
}

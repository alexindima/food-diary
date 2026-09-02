using System.Reflection;
using System.Runtime.CompilerServices;

namespace FoodDiary.Domain.Primitives.Tests;

[ExcludeFromCodeCoverage]
public sealed class EntityAndAggregateRootTests {
    [Fact]
    public void Entity_Equals_SameReference_ReturnsTrue() {
        var entity = TestEntity.Transient();

        bool equals = entity.Equals(entity);

        Assert.True(equals);
    }

    [Fact]
    public void DomainEvent_EventType_DefaultsToConcreteTypeName() {
        IDomainEvent domainEvent = new TestDomainEvent(DateTime.UtcNow);

        Assert.Equal(nameof(TestDomainEvent), domainEvent.EventType);
    }

    [Fact]
    public void DomainEvent_EventType_AllowsStableOverride() {
        IDomainEvent domainEvent = new NamedTestDomainEvent(DateTime.UtcNow);

        Assert.Equal("test-event", domainEvent.EventType);
    }

    [Fact]
    public void Entity_Equals_NullEntity_ReturnsFalse() {
        var entity = TestEntity.WithId(Guid.NewGuid());

        bool equals = entity.Equals((Entity<Guid>?)null);

        Assert.False(equals);
    }

    [Fact]
    public void Entity_EqualsObject_NullObject_ReturnsFalse() {
        var entity = TestEntity.WithId(Guid.NewGuid());

        bool equals = entity.Equals((object?)null);

        Assert.False(equals);
    }

    [Fact]
    public void Entity_EqualsObject_DifferentType_ReturnsFalse() {
        var entity = TestEntity.WithId(Guid.NewGuid());

        bool equals = entity.Equals(new object());

        Assert.False(equals);
    }

    [Fact]
    public void Entity_EqualsObject_SameReference_ReturnsTrue() {
        var entity = TestEntity.Transient();

        bool equals = entity.Equals((object)entity);

        Assert.True(equals);
    }

    [Fact]
    public void Entity_Equals_SameNonDefaultIdSameType_ReturnsTrue() {
        var id = Guid.NewGuid();
        var left = TestEntity.WithId(id);
        var right = TestEntity.WithId(id);

        Assert.True(left.Equals(right));
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Entity_Equals_SameIdDifferentType_ReturnsFalse() {
        var id = Guid.NewGuid();
        var left = TestEntity.WithId(id);
        var right = AnotherTestEntity.WithId(id);

        Assert.False(left.Equals(right));
    }

    [Fact]
    public void Entity_Equals_TwoTransientEntities_ReturnsFalse() {
        var left = TestEntity.Transient();
        var right = TestEntity.Transient();

        Assert.False(left.Equals(right));
        Assert.True(left != right);
        Assert.Equal(RuntimeHelpers.GetHashCode(left), left.GetHashCode());
        Assert.Equal(RuntimeHelpers.GetHashCode(right), right.GetHashCode());
    }

    [Fact]
    public void Entity_Equals_TransientAndPersisted_ReturnsFalse() {
        var transient = TestEntity.Transient();
        var persisted = TestEntity.WithId(Guid.NewGuid());

        Assert.False(transient.Equals(persisted));
        Assert.True(transient != persisted);
    }

    [Fact]
    public void Entity_Operators_HandleNullSides() {
        TestEntity? left = null;
        var right = TestEntity.WithId(Guid.NewGuid());

        Assert.False(left == right);
        Assert.True(left != right);
    }

    [Fact]
    public void Entity_GetHashCode_Transient_UsesRuntimeHashForSameInstance() {
        var entity = TestEntity.Transient();

        int hashCode = entity.GetHashCode();

        Assert.Equal(RuntimeHelpers.GetHashCode(entity), hashCode);
        Assert.Equal(hashCode, entity.GetHashCode());
    }

    [Fact]
    public void Entity_Id_IsInitOnly() {
        MethodInfo setter = typeof(Entity<Guid>).GetProperty(nameof(Entity<Guid>.Id))!.SetMethod!;

        Assert.Contains(typeof(IsExternalInit), setter.ReturnParameter.GetRequiredCustomModifiers());
    }

    [Fact]
    public void Entity_Id_InitializedWithDefaultValue_RemainsTransient() {
        var entity = TestEntity.Initialized(Guid.Empty);

        Assert.Equal(RuntimeHelpers.GetHashCode(entity), entity.GetHashCode());
    }

    [Fact]
    public void Entity_GetHashCode_PersistedMaterializedEntity_CachesHashAfterFirstCall() {
        var entity = TestEntity.Materialized(Guid.NewGuid());

        int first = entity.GetHashCode();
        int second = entity.GetHashCode();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Entity_GetHashCode_PersistedEntityWithoutCachedHash_CachesHashAfterFirstCall() {
        var id = Guid.NewGuid();
        var entity = TestEntity.MaterializedWithoutCachedHash(id);

        int hashCode = entity.GetHashCode();

        Assert.Equal(HashCode.Combine(typeof(TestEntity), EqualityComparer<Guid>.Default.GetHashCode(id)), hashCode);
        Assert.Equal(hashCode, entity.GetHashCode());
    }

    [Fact]
    public void Entity_SetCreated_SetsCreatedOnUtc() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        DateTime before = DateTime.UtcNow;

        entity.MarkCreated();

        Assert.True(entity.CreatedOnUtc >= before);
        Assert.Equal(DateTimeKind.Utc, entity.CreatedOnUtc.Kind);
    }

    [Fact]
    public void Entity_SetCreated_WithLocalTime_Throws() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var localTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Local);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => entity.MarkCreated(localTime));

        Assert.Equal("createdOnUtc", ex.ParamName);
    }

    [Fact]
    public void Entity_SetCreated_WithUnspecifiedTime_Throws() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var unspecifiedTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Unspecified);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => entity.MarkCreated(unspecifiedTime));

        Assert.Equal("createdOnUtc", ex.ParamName);
    }

    [Fact]
    public void Entity_SetModified_SetsModifiedOnUtc() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        entity.MarkCreated();

        entity.MarkModified();

        Assert.NotNull(entity.ModifiedOnUtc);
        Assert.True(entity.ModifiedOnUtc >= entity.CreatedOnUtc);
        Assert.Equal(DateTimeKind.Utc, entity.ModifiedOnUtc?.Kind);
    }

    [Fact]
    public void Entity_SetModified_WithLocalTime_Throws() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var localTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Local);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => entity.MarkModified(localTime));

        Assert.Equal("modifiedOnUtc", ex.ParamName);
    }

    [Fact]
    public void Entity_SetModified_WithUnspecifiedTime_Throws() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var unspecifiedTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Unspecified);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => entity.MarkModified(unspecifiedTime));

        Assert.Equal("modifiedOnUtc", ex.ParamName);
    }

    [Fact]
    public void AggregateRoot_RaiseDomainEvent_AddsEvent() {
        var aggregate = TestAggregateRoot.WithId(Guid.NewGuid());
        var @event = new TestDomainEvent(DateTime.UtcNow);

        aggregate.AddEvent(@event);

        IDomainEvent single = Assert.Single(aggregate.DomainEvents);
        Assert.Same(@event, single);
    }

    [Fact]
    public void AggregateRoot_RaiseDomainEvent_WithNull_Throws() {
        var aggregate = TestAggregateRoot.WithId(Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() => aggregate.AddEvent(null!));
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AggregateRoot_RaisesEvents_InOrder() {
        var aggregate = TestAggregateRoot.WithId(Guid.NewGuid());
        var first = new TestDomainEvent(DateTime.UtcNow.AddSeconds(-1));
        var second = new TestDomainEvent(DateTime.UtcNow);

        aggregate.AddEvent(first);
        aggregate.AddEvent(second);

        Assert.Collection(
            aggregate.DomainEvents,
            item => Assert.Same(first, item),
            item => Assert.Same(second, item));
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_EmptiesCollection() {
        var aggregate = TestAggregateRoot.WithId(Guid.NewGuid());
        aggregate.AddEvent(new TestDomainEvent(DateTime.UtcNow));
        Assert.NotEmpty(aggregate.DomainEvents);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AggregateRoot_DefaultConstructor_CreatesTransientAggregate() {
        var aggregate = new TestAggregateRoot();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainTime_UtcNow_ReturnsUtcTimestamp() {
        DateTime value = DomainTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, value.Kind);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void DomainTime_EnsureUtc_WithNonUtcKind_Throws(DateTimeKind kind) {
        var value = new DateTime(2026, 6, 3, 12, 30, 0, kind);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => DomainTime.EnsureUtc(value, "value"));

        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void DomainTime_EnsureUtc_WithUtcValue_ReturnsSameValue() {
        var value = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Utc);

        DateTime result = DomainTime.EnsureUtc(value, "value");

        Assert.Equal(value, result);
    }

    [Fact]
    public void DomainTime_Override_UsesScopedProviderAndRestoresPreviousProvider() {
        var outerTime = new DateTimeOffset(2026, 6, 3, 8, 30, 0, TimeSpan.Zero);
        DateTimeOffset innerTime = outerTime.AddHours(1);
        using IDisposable outerScope = DomainTime.Override(new FixedTimeProvider(outerTime));
        Assert.Equal(outerTime.UtcDateTime, DomainTime.UtcNow);

        IDisposable innerScope = DomainTime.Override(new FixedTimeProvider(innerTime));
        Assert.Equal(innerTime.UtcDateTime, DomainTime.UtcNow);

        innerScope.Dispose();
        innerScope.Dispose();

        Assert.Equal(outerTime.UtcDateTime, DomainTime.UtcNow);
    }

    [Fact]
    public async Task DomainTime_Override_IsolatedAcrossParallelAsyncFlows() {
        var firstTime = new DateTimeOffset(2026, 6, 3, 8, 30, 0, TimeSpan.Zero);
        DateTimeOffset secondTime = firstTime.AddDays(1);

        Task<DateTime> first = Task.Run(() => ReadOverriddenTime(firstTime));
        Task<DateTime> second = Task.Run(() => ReadOverriddenTime(secondTime));

        DateTime[] results = await Task.WhenAll(first, second);

        Assert.Equal(firstTime.UtcDateTime, results[0]);
        Assert.Equal(secondTime.UtcDateTime, results[1]);
    }

    [Fact]
    public void DomainTime_Override_WithNullProvider_Throws() {
        Assert.Throws<ArgumentNullException>(() => DomainTime.Override(null!));
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestEntity : Entity<Guid> {
        private TestEntity() {
        }

        private TestEntity(Guid id) : base(id) {
        }

        public static TestEntity WithId(Guid id) => new(id);

        public static TestEntity Initialized(Guid id) => new() { Id = id };

        public static TestEntity Transient() => new();

        public static TestEntity Materialized(Guid id) {
            var entity = new TestEntity();
            typeof(TestEntity)
                .GetProperty(nameof(Id))!
                .SetValue(entity, id);
            return entity;
        }

        public static TestEntity MaterializedWithoutCachedHash(Guid id) {
            var entity = new TestEntity();
            typeof(TestEntity)
                .GetProperty(nameof(Id))!
                .SetValue(entity, id);
            typeof(Entity<Guid>)
                .GetField("_cachedHashCode", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(entity, value: null);
            return entity;
        }

        public void MarkCreated() => SetCreated();

        public void MarkCreated(DateTime createdOnUtc) => SetCreated(createdOnUtc);

        public void MarkModified() => SetModified();

        public void MarkModified(DateTime modifiedOnUtc) => SetModified(modifiedOnUtc);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AnotherTestEntity : Entity<Guid> {
        private AnotherTestEntity(Guid id) : base(id) {
        }

        public static AnotherTestEntity WithId(Guid id) => new(id);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAggregateRoot : AggregateRoot<Guid> {
        public TestAggregateRoot() {
        }

        private TestAggregateRoot(Guid id) : base(id) {
        }

        public static TestAggregateRoot WithId(Guid id) => new(id);

        public void AddEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestDomainEvent(DateTime OccurredOnUtc) : IDomainEvent;

    [ExcludeFromCodeCoverage]
    private sealed record NamedTestDomainEvent(DateTime OccurredOnUtc) : IDomainEvent {
        public string EventType => "test-event";
    }

    private static DateTime ReadOverriddenTime(DateTimeOffset utcNow) {
        using IDisposable scope = DomainTime.Override(new FixedTimeProvider(utcNow));
        return DomainTime.UtcNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

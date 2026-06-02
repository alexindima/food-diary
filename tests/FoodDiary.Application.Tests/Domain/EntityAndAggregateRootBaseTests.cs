using FoodDiary.Domain.Common;

namespace FoodDiary.Application.Tests.Domain;

public class EntityAndAggregateRootBaseTests {
    [Fact]
    public void Entity_Equals_SameReference_ReturnsTrue() {
        var entity = TestEntity.Transient();

        var equals = entity.Equals(entity);

        Assert.True(equals);
    }

    [Fact]
    public void Entity_Equals_NullEntity_ReturnsFalse() {
        var entity = TestEntity.WithId(Guid.NewGuid());

        var equals = entity.Equals((Entity<Guid>?)null);

        Assert.False(equals);
    }

    [Fact]
    public void Entity_EqualsObject_NullObject_ReturnsFalse() {
        var entity = TestEntity.WithId(Guid.NewGuid());

        var equals = entity.Equals((object?)null);

        Assert.False(equals);
    }

    [Fact]
    public void Entity_EqualsObject_DifferentType_ReturnsFalse() {
        var entity = TestEntity.WithId(Guid.NewGuid());

        var equals = entity.Equals(new object());

        Assert.False(equals);
    }

    [Fact]
    public void Entity_EqualsObject_SameReference_ReturnsTrue() {
        var entity = TestEntity.Transient();

        var equals = entity.Equals((object)entity);

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
    }

    [Fact]
    public void Entity_Equals_TransientAndPersisted_ReturnsFalse() {
        var transient = TestEntity.Transient();
        var persisted = TestEntity.WithId(Guid.NewGuid());

        Assert.False(transient.Equals(persisted));
        Assert.True(transient != persisted);
    }

    [Fact]
    public void Entity_GetHashCode_Transient_IsStableForSameInstance() {
        var entity = TestEntity.Transient();

        var first = entity.GetHashCode();
        var second = entity.GetHashCode();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Entity_SetCreated_SetsCreatedOnUtc() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var before = DateTime.UtcNow;

        entity.MarkCreated();

        Assert.True(entity.CreatedOnUtc >= before);
    }

    [Fact]
    public void Entity_SetCreated_WithLocalTime_StoresUtcTime() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var localTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Local);

        entity.MarkCreated(localTime);

        Assert.Equal(localTime.ToUniversalTime(), entity.CreatedOnUtc);
        Assert.Equal(DateTimeKind.Utc, entity.CreatedOnUtc.Kind);
    }

    [Fact]
    public void Entity_SetCreated_WithUnspecifiedTime_Throws() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var unspecifiedTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Unspecified);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => entity.MarkCreated(unspecifiedTime));

        Assert.Equal("createdOnUtc", ex.ParamName);
    }

    [Fact]
    public void Entity_SetModified_SetsModifiedOnUtc() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        entity.MarkCreated();

        entity.MarkModified();

        Assert.NotNull(entity.ModifiedOnUtc);
        Assert.True(entity.ModifiedOnUtc >= entity.CreatedOnUtc);
    }

    [Fact]
    public void Entity_SetModified_WithLocalTime_StoresUtcTime() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var localTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Local);

        entity.MarkModified(localTime);

        Assert.Equal(localTime.ToUniversalTime(), entity.ModifiedOnUtc);
        Assert.Equal(DateTimeKind.Utc, entity.ModifiedOnUtc?.Kind);
    }

    [Fact]
    public void Entity_SetModified_WithUnspecifiedTime_Throws() {
        var entity = TestEntity.WithId(Guid.NewGuid());
        var unspecifiedTime = new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Unspecified);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => entity.MarkModified(unspecifiedTime));

        Assert.Equal("modifiedOnUtc", ex.ParamName);
    }

    [Fact]
    public void AggregateRoot_RaiseDomainEvent_AddsEvent() {
        var aggregate = TestAggregateRoot.WithId(Guid.NewGuid());
        var @event = new TestDomainEvent(DateTime.UtcNow);

        aggregate.AddEvent(@event);

        var single = Assert.Single(aggregate.DomainEvents);
        Assert.Same(@event, single);
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

    private sealed class TestEntity : Entity<Guid> {
        private TestEntity() {
        }

        private TestEntity(Guid id) : base(id) {
        }

        public static TestEntity WithId(Guid id) => new(id);

        public static TestEntity Transient() => new();

        public void MarkCreated() => SetCreated();

        public void MarkCreated(DateTime createdOnUtc) => SetCreated(createdOnUtc);

        public void MarkModified() => SetModified();

        public void MarkModified(DateTime modifiedOnUtc) => SetModified(modifiedOnUtc);
    }

    private sealed class AnotherTestEntity : Entity<Guid> {
        private AnotherTestEntity(Guid id) : base(id) {
        }

        public static AnotherTestEntity WithId(Guid id) => new(id);
    }

    private sealed class TestAggregateRoot : AggregateRoot<Guid> {
        private TestAggregateRoot(Guid id) : base(id) {
        }

        public static TestAggregateRoot WithId(Guid id) => new(id);

        public void AddEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent(DateTime OccurredOnUtc) : IDomainEvent;
}

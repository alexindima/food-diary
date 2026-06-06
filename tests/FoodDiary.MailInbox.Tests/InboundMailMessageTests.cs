using FoodDiary.MailInbox.Domain.Events;
using FoodDiary.MailInbox.Domain.Common;
using FoodDiary.MailInbox.Domain.Messages;
using System.Globalization;

namespace FoodDiary.MailInbox.Tests;

[ExcludeFromCodeCoverage]
public sealed class InboundMailMessageTests {
    [Fact]
    public void Receive_WhenValuesAreValid_CreatesReceivedAggregate() {
        var receivedAtUtc = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

        var message = InboundMailMessage.Receive(
            " message-id ",
            " sender@example.com ",
            [" admin@fooddiary.club "],
            " subject ",
            "text",
            "<p>html</p>",
            "raw",
            receivedAtUtc);

        Assert.NotEqual(Guid.Empty, message.Id.Value);
        Assert.Equal("message-id", message.MessageId);
        Assert.Equal("sender@example.com", message.FromAddress);
        Assert.Equal(["admin@fooddiary.club"], message.ToRecipients);
        Assert.Equal("subject", message.Subject);
        Assert.Equal(InboundMailMessageStatus.Received, message.Status);
        Assert.Equal(receivedAtUtc, message.ReceivedAtUtc);
        Assert.Single(message.DomainEvents);
        Assert.IsType<InboundMailMessageReceivedDomainEvent>(message.DomainEvents[0]);
    }

    [Fact]
    public void Archive_WhenMessageIsReceived_ChangesStatus() {
        var message = InboundMailMessage.Receive(
            null,
            null,
            ["admin@fooddiary.club"],
            null,
            null,
            null,
            "raw",
            DateTimeOffset.UtcNow);

        message.Archive(DateTimeOffset.UtcNow);

        Assert.Equal(InboundMailMessageStatus.Archived, message.Status);
        Assert.NotNull(message.ModifiedOnUtc);
    }

    [Fact]
    public void Archive_WhenMessageIsAlreadyArchived_DoesNotChangeModifiedTimestamp() {
        InboundMailMessage message = CreateMessage();
        var archivedAtUtc = new DateTimeOffset(2026, 4, 26, 10, 0, 0, TimeSpan.Zero);

        message.Archive(archivedAtUtc);
        DateTime? modifiedOnUtc = message.ModifiedOnUtc;
        message.Archive(archivedAtUtc.AddHours(1));

        Assert.Equal(InboundMailMessageStatus.Archived, message.Status);
        Assert.Equal(modifiedOnUtc, message.ModifiedOnUtc);
    }

    [Fact]
    public void Receive_WhenRecipientsAreEmpty_Throws() {
        Assert.Throws<ArgumentException>(() => InboundMailMessage.Receive(
            null,
            null,
            [],
            null,
            null,
            null,
            "raw",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Receive_WhenRecipientIsWhiteSpace_Throws() {
        Assert.Throws<ArgumentException>(() => InboundMailMessage.Receive(
            null,
            null,
            [" "],
            null,
            null,
            null,
            "raw",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Receive_WhenRawMimeIsWhiteSpace_Throws() {
        Assert.Throws<ArgumentException>(() => InboundMailMessage.Receive(
            null,
            null,
            ["admin@fooddiary.club"],
            null,
            null,
            null,
            " ",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Receive_NormalizesReceivedAtToUtcAndNullsWhiteSpaceFields() {
        var receivedAt = new DateTimeOffset(2026, 4, 26, 13, 0, 0, TimeSpan.FromHours(3));

        var message = InboundMailMessage.Receive(
            " ",
            " ",
            ["admin@fooddiary.club"],
            " ",
            null,
            null,
            "raw",
            receivedAt);

        Assert.Null(message.MessageId);
        Assert.Null(message.FromAddress);
        Assert.Null(message.Subject);
        Assert.Equal(
            DateTimeOffset.Parse("2026-04-26T10:00:00+00:00", CultureInfo.InvariantCulture),
            message.ReceivedAtUtc);
        Assert.Equal(DateTimeKind.Utc, message.CreatedOnUtc.Kind);
    }

    [Fact]
    public void ClearDomainEvents_RemovesRaisedEvents() {
        InboundMailMessage message = CreateMessage();

        message.ClearDomainEvents();

        Assert.Empty(message.DomainEvents);
    }

    [Fact]
    public void InboundMailMessageId_ConvertsToAndFromGuid() {
        var value = Guid.NewGuid();

        var id = (InboundMailMessageId)value;
        Guid converted = id;

        Assert.Equal(value, id.Value);
        Assert.Equal(value, converted);
        Assert.Equal(Guid.Empty, InboundMailMessageId.Empty.Value);
        Assert.NotEqual(Guid.Empty, InboundMailMessageId.New().Value);
    }

    [Fact]
    public void InboundMailMessageStatus_FromKnownValues_ReturnsStatus() {
        Assert.Equal(InboundMailMessageStatus.Received, InboundMailMessageStatus.From("received"));
        Assert.Equal(InboundMailMessageStatus.Archived, InboundMailMessageStatus.From("archived"));
        Assert.Equal("received", InboundMailMessageStatus.Received.ToString());
    }

    [Fact]
    public void InboundMailMessageStatus_FromUnknownValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => InboundMailMessageStatus.From("unknown"));
    }

    [Fact]
    public void InboundMailMessageReceivedDomainEvent_NormalizesOccurredOnToUtc() {
        var id = InboundMailMessageId.New();
        var occurredAt = new DateTime(2026, 4, 26, 13, 0, 0, DateTimeKind.Local);

        var domainEvent = new InboundMailMessageReceivedDomainEvent(id, occurredAt);

        Assert.Equal(id, domainEvent.MessageId);
        Assert.Equal(DateTimeKind.Utc, domainEvent.OccurredOnUtc.Kind);
    }

    [Fact]
    public void EntityEquality_UsesTypeAndNonTransientId() {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);
        var different = new TestEntity(Guid.NewGuid());
        var otherType = new OtherTestEntity(id);

        Assert.True(first.Equals(second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.False(first.Equals(different));
        Assert.False(first.Equals(otherType));
        Assert.False(first.Equals(null));
        Assert.False(first.Equals(new object()));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void EntityObjectEquality_HandlesNullReferenceAndDifferentRuntimeType() {
        var entity = new TestEntity(Guid.NewGuid());
        object? nullObject = null;
        object otherType = new OtherTestEntity(entity.Id);

        Assert.False(entity.Equals(nullObject));
        Assert.False(entity.Equals(otherType));
    }

    [Fact]
    public void EntityGetHashCode_WhenNonTransient_CachesComputedHashCode() {
        var entity = new TestEntity(Guid.NewGuid());

        int firstHashCode = entity.GetHashCode();
        int secondHashCode = entity.GetHashCode();

        Assert.Equal(firstHashCode, secondHashCode);
    }

    [Fact]
    public void EntityGetHashCode_WhenMaterializedWithoutCachedHashCode_ComputesAndCachesHashCode() {
        var entity = new TestEntity();
        var id = Guid.NewGuid();
        Type entityType = typeof(Entity<Guid>);
        entityType.GetField(
            "_id",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(entity, id);
        entityType.GetField(
            "_cachedHashCode",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(entity, null);

        int hashCode = entity.GetHashCode();

        Assert.Equal(
            HashCode.Combine(typeof(TestEntity), EqualityComparer<Guid>.Default.GetHashCode(id)),
            hashCode);
        Assert.Equal(hashCode, entity.GetHashCode());
    }

    [Fact]
    public void EntityEquality_TreatsTransientEntitiesAsDifferent() {
        var first = new TestEntity();
        var second = new TestEntity();

        Assert.False(first.Equals(second));
        Assert.NotEqual(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void EntityEquality_TreatsSameTransientReferenceAsEqual() {
        var entity = new TestEntity();
        TestEntity same = entity;

        Assert.True(entity.Equals(entity));
        Assert.True(entity.Equals((object)entity));
        Assert.True(entity == same);
        Assert.False(entity != same);
    }

    [Fact]
    public void EntityEquality_WhenLeftSideIsNull_UsesOperators() {
        TestEntity? left = null;
        var right = new TestEntity(Guid.NewGuid());

        Assert.False(left == right);
        Assert.True(left != right);
    }

    [Fact]
    public void EntitySetCreated_UsesCurrentUtcTime() {
        var entity = new TestEntity(Guid.NewGuid());

        entity.SetCreatedPublic();

        Assert.Equal(DateTimeKind.Utc, entity.CreatedOnUtc.Kind);
        Assert.NotEqual(default, entity.CreatedOnUtc);
    }

    [Fact]
    public void EntitySetModified_UsesCurrentUtcTime() {
        var entity = new TestEntity(Guid.NewGuid());

        entity.SetModifiedPublic();

        Assert.Equal(DateTimeKind.Utc, entity.ModifiedOnUtc?.Kind);
    }

    [Fact]
    public void EntitySetCreated_WhenKindIsUnspecified_Throws() {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => entity.SetCreatedPublic(new DateTime(2026, 4, 26)));
    }

    [Fact]
    public void EntitySetModified_NormalizesTimestampToUtc() {
        var entity = new TestEntity(Guid.NewGuid());
        var timestamp = new DateTime(2026, 4, 26, 13, 0, 0, DateTimeKind.Local);

        entity.SetCreatedPublic(DateTime.UtcNow);
        entity.SetModifiedPublic(timestamp);

        Assert.Equal(DateTimeKind.Utc, entity.ModifiedOnUtc?.Kind);
    }

    [Fact]
    public void DomainTime_UtcNow_ReturnsUtcTimestamp() {
        Type? type = typeof(InboundMailMessage).Assembly.GetType("FoodDiary.MailInbox.Domain.Common.DomainTime");
        var value = (DateTime)type!.GetProperty(
            "UtcNow",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!.GetValue(null)!;

        Assert.Equal(DateTimeKind.Utc, value.Kind);
    }

    private static InboundMailMessage CreateMessage() =>
        InboundMailMessage.Receive(
            null,
            null,
            ["admin@fooddiary.club"],
            null,
            null,
            null,
            "raw",
            DateTimeOffset.UtcNow);

    [ExcludeFromCodeCoverage]
    private sealed class TestEntity : Entity<Guid> {
        public TestEntity() {
        }

        public TestEntity(Guid id) : base(id) {
        }

        public void SetCreatedPublic(DateTime createdOnUtc) {
            SetCreated(createdOnUtc);
        }

        public void SetCreatedPublic() {
            SetCreated();
        }

        public void SetModifiedPublic(DateTime modifiedOnUtc) {
            SetModified(modifiedOnUtc);
        }

        public void SetModifiedPublic() {
            SetModified();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class OtherTestEntity(Guid id) : Entity<Guid>(id);
}

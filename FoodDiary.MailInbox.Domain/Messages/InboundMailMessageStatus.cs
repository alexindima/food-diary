namespace FoodDiary.MailInbox.Domain.Messages;

public readonly record struct InboundMailMessageStatus {
    public static readonly InboundMailMessageStatus Received = new("received");
    public static readonly InboundMailMessageStatus Archived = new("archived");

    public string Value { get; }

    private InboundMailMessageStatus(string value) {
        Value = value;
    }

    public static InboundMailMessageStatus From(string value) {
        return value switch {
            "received" => Received,
            "archived" => Archived,
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown inbound mail message status.")
        };
    }

    public override string ToString() {
        return Value;
    }
}

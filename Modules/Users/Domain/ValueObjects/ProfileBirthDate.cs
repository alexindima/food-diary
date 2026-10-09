namespace FoodDiary.Modules.Users.Domain.ValueObjects;

/// <summary>Birth-date meaning with the original profile/storage encoding retained for its owning policy.</summary>
public sealed record ProfileBirthDate {
    public DateTime EncodedDateTime { get; }
    public DateOnly CalendarDay => DateOnly.FromDateTime(EncodedDateTime);

    private ProfileBirthDate(DateTime encodedDateTime) => EncodedDateTime = encodedDateTime;

    // Decoding does not move normalization/future validation ahead of the User mutation facade.
    public static ProfileBirthDate FromEncodedDateTime(DateTime value) => new(value);
}

using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Dietologist.Common;

public static class DietologistErrors {
    public static Error InvitationNotFound => new(
        "Dietologist.InvitationNotFound",
        "Dietologist invitation was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvitationExpired => new(
        "Dietologist.InvitationExpired",
        "Dietologist invitation has expired.",
        Kind: ErrorKind.Validation);

    public static Error InvitationInvalidToken => new(
        "Dietologist.InvitationInvalidToken",
        "Invitation token is invalid.",
        Kind: ErrorKind.Unauthorized);

    public static Error AlreadyHasDietologist => new(
        "Dietologist.AlreadyHasDietologist",
        "You already have an active dietologist.",
        Kind: ErrorKind.Conflict);

    public static Error PendingInvitationExists => new(
        "Dietologist.PendingInvitationExists",
        "A pending invitation already exists.",
        Kind: ErrorKind.Conflict);

    public static Error CannotInviteSelf => new(
        "Dietologist.CannotInviteSelf",
        "You cannot invite yourself as a dietologist.",
        Kind: ErrorKind.Validation);

    public static Error AccessDenied => new(
        "Dietologist.AccessDenied",
        "You do not have access to this client's data.",
        Kind: ErrorKind.Forbidden);

    public static Error PermissionDenied => new(
        "Dietologist.PermissionDenied",
        "The client has not shared this data category.",
        Kind: ErrorKind.Forbidden);

    public static Error NoActiveRelationship => new(
        "Dietologist.NoActiveRelationship",
        "No active dietologist relationship found.",
        Kind: ErrorKind.NotFound);
}

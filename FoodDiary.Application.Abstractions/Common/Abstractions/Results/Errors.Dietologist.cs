using FoodDiary.Application.Abstractions.Dietologist.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Dietologist {
        public static Error InvitationNotFound => DietologistErrors.InvitationNotFound;

        public static Error InvitationExpired => DietologistErrors.InvitationExpired;

        public static Error InvitationInvalidToken => DietologistErrors.InvitationInvalidToken;

        public static Error AlreadyHasDietologist => DietologistErrors.AlreadyHasDietologist;

        public static Error PendingInvitationExists => DietologistErrors.PendingInvitationExists;

        public static Error CannotInviteSelf => DietologistErrors.CannotInviteSelf;

        public static Error AccessDenied => DietologistErrors.AccessDenied;

        public static Error PermissionDenied => DietologistErrors.PermissionDenied;

        public static Error NoActiveRelationship => DietologistErrors.NoActiveRelationship;
    }
}

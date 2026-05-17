export type DietologistPermissions = {
    shareProfile: boolean;
    shareMeals: boolean;
    shareStatistics: boolean;
    shareWeight: boolean;
    shareWaist: boolean;
    shareGoals: boolean;
    shareHydration: boolean;
    shareFasting: boolean;
};

export type ClientSummary = {
    userId: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    profileImage: string | null;
    birthDate: string | null;
    gender: string | null;
    height: number | null;
    activityLevel: string | null;
    permissions: DietologistPermissions;
    acceptedAtUtc: string;
};

export type DietologistRelationship = {
    invitationId: string;
    status: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    dietologistUserId: string | null;
    permissions: DietologistPermissions;
    createdAtUtc: string;
    expiresAtUtc: string;
    acceptedAtUtc: string | null;
};

export type DietologistInvitationForCurrentUser = {
    invitationId: string;
    clientUserId: string;
    clientEmail: string;
    clientFirstName: string | null;
    clientLastName: string | null;
    status: string;
    createdAtUtc: string;
    expiresAtUtc: string;
};

export type InviteDietologistRequest = {
    dietologistEmail: string;
    permissions: DietologistPermissions;
};

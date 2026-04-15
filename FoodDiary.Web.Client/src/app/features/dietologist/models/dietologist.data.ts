export interface DietologistPermissions {
    shareMeals: boolean;
    shareStatistics: boolean;
    shareWeight: boolean;
    shareWaist: boolean;
    shareGoals: boolean;
    shareHydration: boolean;
}

export interface ClientSummary {
    userId: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    permissions: DietologistPermissions;
    acceptedAtUtc: string;
}

export interface DietologistInfo {
    invitationId: string;
    dietologistUserId: string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    permissions: DietologistPermissions;
    acceptedAtUtc: string;
}

export interface DietologistRelationship {
    invitationId: string;
    status: 'Pending' | 'Accepted' | string;
    email: string;
    firstName: string | null;
    lastName: string | null;
    dietologistUserId: string | null;
    permissions: DietologistPermissions;
    createdAtUtc: string;
    expiresAtUtc: string;
    acceptedAtUtc: string | null;
}

export interface DietologistInvitationForCurrentUser {
    invitationId: string;
    clientUserId: string;
    clientEmail: string;
    clientFirstName: string | null;
    clientLastName: string | null;
    status: string;
    createdAtUtc: string;
    expiresAtUtc: string;
}

export interface InviteDietologistRequest {
    dietologistEmail: string;
    permissions: DietologistPermissions;
}

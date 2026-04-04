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

export interface InviteDietologistRequest {
    dietologistEmail: string;
    permissions: DietologistPermissions;
}

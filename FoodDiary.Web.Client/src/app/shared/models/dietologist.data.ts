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

export type DietologistClientGoals = {
    id: string;
    email: string;
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
    waterGoal?: number | null;
    hydrationGoal?: number | null;
    desiredWeight?: number | null;
    desiredWaist?: number | null;
    stepGoal?: number | null;
};

export type DietologistRecommendation = {
    id: string;
    dietologistUserId: string;
    dietologistFirstName: string | null;
    dietologistLastName: string | null;
    text: string;
    isRead: boolean;
    createdAtUtc: string;
    readAtUtc: string | null;
};

export type CreateRecommendationRequest = {
    text: string;
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

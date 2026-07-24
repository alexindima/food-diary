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

export type RecommendationComment = {
    id: string;
    recommendationId: string;
    authorUserId: string;
    authorFirstName: string | null;
    authorLastName: string | null;
    authorEmail: string;
    text: string;
    createdAtUtc: string;
};

export type CreateRecommendationCommentRequest = {
    text: string;
};

export type ClientTaskStatus = 'Open' | 'Completed' | 'Cancelled';

export type ClientTask = {
    id: string;
    dietologistUserId: string;
    clientUserId: string;
    title: string;
    details: string | null;
    dueAtUtc: string | null;
    status: ClientTaskStatus;
    isOverdue: boolean;
    createdAtUtc: string;
    statusChangedAtUtc: string | null;
};

export type CreateClientTaskRequest = {
    title: string;
    details?: string | null;
    dueAtUtc?: string | null;
};

export type RecommendationTemplate = {
    id: string;
    name: string;
    text: string;
    isArchived: boolean;
    createdAtUtc: string;
    modifiedAtUtc: string | null;
};

export type RecommendationTemplateRequest = {
    name: string;
    text: string;
};

export type BulkRecommendationRecipientResult = {
    clientUserId: string;
    succeeded: boolean;
    recommendationId: string | null;
    wasAlreadyProcessed: boolean;
    errorCode: string | null;
};

export type BulkRecommendationResult = {
    idempotencyKey: string;
    recipients: BulkRecommendationRecipientResult[];
};

export type AttentionSignal = {
    id: string;
    clientUserId: string;
    clientDisplayName: string;
    type: 'DiaryInactivity' | 'CalorieTargetDeviation' | 'MaterialWeightChange';
    severity: 'High' | 'Medium' | 'Low';
    reason: 'NoRecentDiaryEntries' | 'InsufficientDiaryData' | 'SustainedCalorieTargetDeviation' | 'MaterialWeightChange';
    detectedAtUtc: string;
    snoozedUntilUtc: string | null;
};

export type AttentionSignalSettings = {
    inactivityDays: number;
    calorieDeviationPercent: number;
    sustainedDays: number;
    weightChangePercent: number;
    lookbackDays: number;
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

import type { CalendarDate, UtcInstant } from './semantics/date-value';
import type {
    AttentionSignalId,
    ClientTaskId,
    DietologistInvitationId,
    RecommendationCommentId,
    RecommendationId,
    RecommendationTemplateId,
    UserId,
} from './semantics/entity-id';
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
    userId: UserId;
    email: string | null;
    firstName: string | null;
    lastName: string | null;
    profileImage: string | null;
    birthDate: CalendarDate | null;
    gender: string | null;
    heightCm: number | null;
    activityLevel: string | null;
    permissions: DietologistPermissions;
    acceptedAtUtc: UtcInstant;
};

export type DietologistClientGoals = {
    id: UserId;
    email: string | null;
    dailyCalorieTarget?: number | null;
    proteinTarget?: number | null;
    fatTarget?: number | null;
    carbTarget?: number | null;
    fiberTarget?: number | null;
    waterGoal?: number | null;
    hydrationGoal?: number | null;
    desiredWeightKg?: number | null;
    desiredWaistCm?: number | null;
    stepGoal?: number | null;
};

export type DietologistRecommendation = {
    id: RecommendationId;
    dietologistUserId: UserId;
    dietologistFirstName: string | null;
    dietologistLastName: string | null;
    text: string;
    isRead: boolean;
    createdAtUtc: UtcInstant;
    readAtUtc: UtcInstant | null;
};

export type CreateRecommendationRequest = {
    text: string;
};

export type RecommendationComment = {
    id: RecommendationCommentId;
    recommendationId: RecommendationId;
    authorUserId: UserId;
    authorFirstName: string | null;
    authorLastName: string | null;
    authorEmail: string | null;
    text: string;
    createdAtUtc: UtcInstant;
};

export type CreateRecommendationCommentRequest = {
    text: string;
};

export type ClientTaskStatus = 'Open' | 'Completed' | 'Cancelled';

export type ClientTask = {
    id: ClientTaskId;
    dietologistUserId: UserId;
    clientUserId: UserId;
    title: string;
    details: string | null;
    dueAtUtc: UtcInstant | null;
    status: ClientTaskStatus;
    isOverdue: boolean;
    createdAtUtc: UtcInstant;
    statusChangedAtUtc: UtcInstant | null;
};

export type CreateClientTaskRequest = {
    title: string;
    details?: string | null;
    dueAtUtc?: string | null;
};

export type RecommendationTemplate = {
    id: RecommendationTemplateId;
    name: string;
    text: string;
    isArchived: boolean;
    createdAtUtc: UtcInstant;
    modifiedAtUtc: UtcInstant | null;
};

export type RecommendationTemplateRequest = {
    name: string;
    text: string;
};

export type BulkRecommendationRecipientResult = {
    clientUserId: UserId;
    succeeded: boolean;
    recommendationId: RecommendationId | null;
    wasAlreadyProcessed: boolean;
    errorCode: string | null;
};

export type BulkRecommendationResult = {
    idempotencyKey: string;
    recipients: BulkRecommendationRecipientResult[];
};

export type AttentionSignal = {
    id: AttentionSignalId;
    clientUserId: UserId;
    clientDisplayName: string | null;
    type: 'DiaryInactivity' | 'CalorieTargetDeviation' | 'MaterialWeightChange';
    severity: 'High' | 'Medium' | 'Low';
    reason: 'NoRecentDiaryEntries' | 'InsufficientDiaryData' | 'SustainedCalorieTargetDeviation' | 'MaterialWeightChange';
    detectedAtUtc: UtcInstant;
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
    invitationId: DietologistInvitationId;
    status: string;
    email: string | null;
    firstName: string | null;
    lastName: string | null;
    dietologistUserId: UserId | null;
    permissions: DietologistPermissions;
    createdAtUtc: UtcInstant;
    expiresAtUtc: UtcInstant;
    acceptedAtUtc: UtcInstant | null;
};

export type DietologistInvitationForCurrentUser = {
    invitationId: DietologistInvitationId;
    clientUserId: UserId;
    clientEmail: string | null;
    clientFirstName: string | null;
    clientLastName: string | null;
    status: string;
    createdAtUtc: UtcInstant;
    expiresAtUtc: UtcInstant;
};

export type InviteDietologistRequest = {
    dietologistEmail: string;
    permissions: DietologistPermissions;
};

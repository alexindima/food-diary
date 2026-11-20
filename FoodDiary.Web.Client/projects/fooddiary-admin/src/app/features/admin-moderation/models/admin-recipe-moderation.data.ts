import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';

export const RECIPE_MODERATION_STATUSES = [
    'Pending',
    'NeedsReview',
    'ChangesRequested',
    'CheckDelayed',
    'Published',
    'Superseded',
] as const;

export type RecipeModerationStatus = (typeof RECIPE_MODERATION_STATUSES)[number];
export const RECIPE_MODERATION_STATUS_LABELS: Record<RecipeModerationStatus, string> = {
    Pending: 'ADMIN_RECIPE_MODERATION.PENDING',
    NeedsReview: 'ADMIN_RECIPE_MODERATION.NEEDS_REVIEW',
    ChangesRequested: 'ADMIN_RECIPE_MODERATION.CHANGES_REQUESTED',
    CheckDelayed: 'ADMIN_RECIPE_MODERATION.CHECK_DELAYED',
    Published: 'ADMIN_RECIPE_MODERATION.PUBLISHED',
    Superseded: 'ADMIN_RECIPE_MODERATION.SUPERSEDED',
};

export type RecipeModerationRevisionId = AdminId<'recipe-moderation-revision'>;
export type RecipeModerationPolicyId = AdminId<'recipe-moderation-policy'>;

export type AdminRecipeModerationImage = {
    assetId: AdminId<'image-asset'>;
    field: string;
};

export type AdminRecipeModerationItem = {
    revisionId: RecipeModerationRevisionId;
    recipeId: AdminId<'recipe'>;
    ownerId: AdminId<'user'>;
    status: RecipeModerationStatus;
    text: string;
    summary: string;
    createdAt: AdminUtcInstant;
    updatedAt: AdminUtcInstant | null;
    policyVersionId: RecipeModerationPolicyId;
    reason: string | null;
    primaryResult: string | null;
    shadowResult: string | null;
    attempts: number;
    images: AdminRecipeModerationImage[];
};

export type AdminRecipeModerationPage = {
    items: AdminRecipeModerationItem[];
    totalItems: number;
    page: number;
    limit: number;
    pendingCount: number;
    needsReviewCount: number;
    delayedCount: number;
};

export type AdminRecipeModerationRules = {
    textReviewCategories: string[];
    imageReviewCategories: string[];
    mistralSamplePercent: number;
};

export type AdminRecipeModerationPolicyVersion = {
    id: RecipeModerationPolicyId;
    publishedAt: AdminUtcInstant;
    publishedBy: AdminId<'user'> | null;
    rules: AdminRecipeModerationRules;
};

export type AdminRecipeModerationPolicy = {
    active: AdminRecipeModerationPolicyVersion;
    history: AdminRecipeModerationPolicyVersion[];
    mistralAvailable: boolean;
};

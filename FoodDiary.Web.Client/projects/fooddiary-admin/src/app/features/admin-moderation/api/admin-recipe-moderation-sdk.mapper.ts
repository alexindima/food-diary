import type { RecipeModerationItem } from '../../../shared/api/sdk/generated/model/recipe-moderation-item';
import type { RecipeModerationPage } from '../../../shared/api/sdk/generated/model/recipe-moderation-page';
import type { RecipeModerationPolicyVersion } from '../../../shared/api/sdk/generated/model/recipe-moderation-policy-version';
import type { RecipeModerationPolicyView } from '../../../shared/api/sdk/generated/model/recipe-moderation-policy-view';
import { requireSdkFields, sdkEnum } from '../../../shared/api/sdk/sdk-response';
import { adminId, adminUtcInstant, optionalAdminId, optionalAdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import {
    type AdminRecipeModerationItem,
    type AdminRecipeModerationPage,
    type AdminRecipeModerationPolicy,
    type AdminRecipeModerationPolicyVersion,
    RECIPE_MODERATION_STATUSES,
} from '../models/admin-recipe-moderation.data';

export function recipeModerationItemFromSdk(response: RecipeModerationItem): AdminRecipeModerationItem {
    const value = requireSdkFields(response, [
        'revisionId',
        'recipeId',
        'ownerId',
        'status',
        'text',
        'createdAt',
        'policyVersionId',
        'attempts',
    ]);
    const [title = ''] = value.text.split(/\r?\n/u, 1);
    return {
        revisionId: adminId<'recipe-moderation-revision'>(value.revisionId),
        recipeId: adminId<'recipe'>(value.recipeId),
        ownerId: adminId<'user'>(value.ownerId),
        status: sdkEnum(value.status, RECIPE_MODERATION_STATUSES),
        text: value.text,
        summary: title.length > 0 ? title : value.recipeId,
        createdAt: adminUtcInstant(value.createdAt),
        updatedAt: optionalAdminUtcInstant(value.updatedAt ?? null),
        policyVersionId: adminId<'recipe-moderation-policy'>(value.policyVersionId),
        reason: value.reason ?? null,
        primaryResult: value.primaryResult ?? null,
        shadowResult: value.shadowResult ?? null,
        attempts: value.attempts,
        images:
            value.images?.map(image => {
                const { assetId, field } = requireSdkFields(image, ['assetId', 'field']);
                return { assetId: adminId<'image-asset'>(assetId), field };
            }) ?? [],
    };
}

export function recipeModerationPageFromSdk(response: RecipeModerationPage): AdminRecipeModerationPage {
    const value = requireSdkFields(response, ['items', 'totalItems', 'page', 'limit', 'pendingCount', 'needsReviewCount', 'delayedCount']);
    return { ...value, items: value.items.map(recipeModerationItemFromSdk) };
}

function recipeModerationPolicyVersionFromSdk(response: RecipeModerationPolicyVersion): AdminRecipeModerationPolicyVersion {
    const value = requireSdkFields(response, ['id', 'publishedAt', 'rules']);
    return {
        id: adminId<'recipe-moderation-policy'>(value.id),
        publishedAt: adminUtcInstant(value.publishedAt),
        publishedBy: optionalAdminId<'user'>(value.publishedBy ?? null),
        rules: requireSdkFields(value.rules, ['textReviewCategories', 'imageReviewCategories', 'mistralSamplePercent']),
    };
}

export function recipeModerationPolicyFromSdk(response: RecipeModerationPolicyView): AdminRecipeModerationPolicy {
    const value = requireSdkFields(response, ['active', 'history', 'mistralAvailable']);
    return {
        active: recipeModerationPolicyVersionFromSdk(value.active),
        history: value.history.map(recipeModerationPolicyVersionFromSdk),
        mistralAvailable: value.mistralAvailable,
    };
}

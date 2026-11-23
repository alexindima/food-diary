import { describe, expect, it } from 'vitest';

import type { RecipeModerationItem } from '../../../shared/api/sdk/generated/model/recipe-moderation-item';
import type { RecipeModerationPolicyView } from '../../../shared/api/sdk/generated/model/recipe-moderation-policy-view';
import {
    recipeModerationItemFromSdk,
    recipeModerationPageFromSdk,
    recipeModerationPolicyFromSdk,
} from './admin-recipe-moderation-sdk.mapper';

const evidence = {
    revisionId: 'b0000000-0000-4000-8000-000000000001',
    recipeId: 'b0000000-0000-4000-8000-000000000002',
    ownerId: 'b0000000-0000-4000-8000-000000000003',
    status: 'ChangesRequested',
    text: 'Lentil soup\r\nAdd carrots and simmer for 15 minutes.',
    createdAt: '2026-11-20T12:30:14.123Z',
    policyVersionId: 'b0000000-0000-4000-8000-000000000004',
    attempts: 2,
} satisfies RecipeModerationItem;

describe('Admin recipe revision SDK mapping', () => {
    it('keeps original evidence, image references and timestamps while extracting the first-line title', () => {
        const details = {
            updatedAt: '2026-11-21T13:45:00.111Z',
            reason: 'Describe the cooking step in more detail.',
            primaryResult: '{"flagged":true,"provider":"primary"}',
            shadowResult: '{"flagged":false,"provider":"shadow"}',
            images: [
                { assetId: 'b0000000-0000-4000-8000-000000000005', field: 'recipe' },
                { assetId: 'b0000000-0000-4000-8000-000000000006', field: 'step/1' },
            ],
        } satisfies RecipeModerationItem;

        expect(recipeModerationItemFromSdk({ ...evidence, ...details })).toEqual({ ...evidence, ...details, summary: 'Lentil soup' });
    });

    it('maps explicit null review metadata for an unchecked submission', () => {
        const response = {
            ...evidence,
            status: 'Pending',
            attempts: 0,
            updatedAt: null,
            reason: null,
            primaryResult: null,
            shadowResult: null,
            images: null,
        } satisfies RecipeModerationItem;

        expect(recipeModerationItemFromSdk(response)).toEqual({ ...response, summary: 'Lentil soup', images: [] });
    });

    it('identifies a revision by recipe when its retained evidence has an empty title line', () => {
        const text = '\nAdd carrots and simmer for 15 minutes.';

        expect(recipeModerationItemFromSdk({ ...evidence, text })).toMatchObject({ summary: evidence.recipeId, text });
    });

    it('rejects a revision when the server sends an unsupported status', () => {
        expect(() => recipeModerationItemFromSdk({ ...evidence, status: 'QueuedForEscalation' })).toThrow('unsupported enum value');
    });

    it('rejects missing evidence metadata before an incomplete image can be requested', () => {
        expect(() => recipeModerationItemFromSdk({ ...evidence, images: [{ field: 'step/1' }] })).toThrow(
            'API response is missing required field: assetId.',
        );
    });

    it('rejects absent queue counts so missing data cannot appear as an empty queue', () => {
        expect(() =>
            recipeModerationPageFromSdk({ items: [], page: 1, limit: 20, totalItems: 0, pendingCount: 0, needsReviewCount: 0 }),
        ).toThrow('API response is missing required field: delayedCount.');
    });
});

describe('Admin recipe policy SDK mapping', () => {
    it('rejects incomplete active rules instead of treating an omitted sample percentage as zero', () => {
        const response = {
            active: {
                id: evidence.policyVersionId,
                publishedAt: '2026-11-20T12:00:00Z',
                publishedBy: null,
                rules: { textReviewCategories: ['harassment'], imageReviewCategories: ['violence'] },
            },
            history: [],
            mistralAvailable: false,
        } satisfies RecipeModerationPolicyView;

        expect(() => recipeModerationPolicyFromSdk(response)).toThrow('API response is missing required field: mistralSamplePercent.');
    });
});

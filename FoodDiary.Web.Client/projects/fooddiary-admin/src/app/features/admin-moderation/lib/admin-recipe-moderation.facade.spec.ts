import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { RecipeModerationItem } from '../../../shared/api/sdk/generated/model/recipe-moderation-item';
import type { RecipeModerationPolicyView } from '../../../shared/api/sdk/generated/model/recipe-moderation-policy-view';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminRecipeModerationService } from '../api/admin-recipe-moderation.service';
import type { AdminRecipeModerationRules } from '../models/admin-recipe-moderation.data';
import { AdminRecipeModerationFacade } from './admin-recipe-moderation.facade';

const moderationUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation`;
const queueUrl = `${moderationUrl}/recipes`;
const rulesUrl = `${moderationUrl}/rules`;
const firstRevision = adminId<'recipe-moderation-revision'>('c0000000-0000-4000-8000-000000000001');
const secondRevision = adminId<'recipe-moderation-revision'>('c0000000-0000-4000-8000-000000000002');
const asset = adminId<'image-asset'>('c0000000-0000-4000-8000-000000000003');
const activePolicy = adminId<'recipe-moderation-policy'>('c0000000-0000-4000-8000-000000000004');
const historicalPolicy = adminId<'recipe-moderation-policy'>('c0a17afe-0737-4fc4-9cf7-4ce3090be7a6');
const newPolicy = adminId<'recipe-moderation-policy'>('c0000000-0000-4000-8000-000000000006');
const restoredPolicy = adminId<'recipe-moderation-policy'>('c0000000-0000-4000-8000-00000000000a');
const publishedBy = 'c0000000-0000-4000-8000-000000000007';
const defaultImageCategories = ['self-harm', 'self-harm/intent', 'self-harm/instructions', 'sexual', 'violence', 'violence/graphic'];
const historicalRules = {
    textReviewCategories: [
        ...defaultImageCategories,
        'harassment',
        'harassment/threatening',
        'hate',
        'hate/threatening',
        'illicit',
        'illicit/violent',
        'sexual/minors',
    ],
    imageReviewCategories: defaultImageCategories,
    mistralSamplePercent: 10,
} satisfies AdminRecipeModerationRules;
const publishedRules = {
    textReviewCategories: ['harassment'],
    imageReviewCategories: ['violence/graphic'],
    mistralSamplePercent: 0,
} satisfies AdminRecipeModerationRules;
const initialVersion = {
    id: activePolicy,
    publishedAt: '2026-11-20T12:30:00Z',
    publishedBy,
    rules: { textReviewCategories: ['hate'], imageReviewCategories: ['violence'], mistralSamplePercent: 10 },
};
const historicalVersion = { id: historicalPolicy, publishedAt: '1970-01-01T00:00:00Z', rules: historicalRules };
const initialPolicy = {
    active: initialVersion,
    history: [initialVersion, historicalVersion],
    mistralAvailable: true,
} satisfies RecipeModerationPolicyView;
const selectedItem = {
    revisionId: secondRevision,
    recipeId: 'c0000000-0000-4000-8000-000000000008',
    ownerId: 'c0000000-0000-4000-8000-000000000009',
    status: 'NeedsReview',
    text: 'Vegetable stew\nCheck the step photo.',
    createdAt: '2026-11-20T13:30:00Z',
    policyVersionId: activePolicy,
    attempts: 1,
    images: [{ assetId: asset, field: 'step/1' }],
} satisfies RecipeModerationItem;

let facade: AdminRecipeModerationFacade;
let http: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [AdminRecipeModerationService, AdminRecipeModerationFacade, provideHttpClient(), provideHttpClientTesting()],
    });
    facade = TestBed.inject(AdminRecipeModerationFacade);
    http = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    http.verify();
});

describe('Admin recipe moderation revision reads', () => {
    it('omits an unset status when reading the unfiltered queue', async () => {
        const empty = { items: [], totalItems: 0, page: 1, limit: 20, pendingCount: 0, needsReviewCount: 0, delayedCount: 0 };
        const result = firstValueFrom(facade.list(empty.page, empty.limit));
        const request = http.expectOne(req => req.url === queueUrl && req.method === 'GET');
        expect(request.request.params.keys().sort()).toEqual(['limit', 'page']);
        request.flush(empty);

        await expect(result).resolves.toMatchObject({ items: [], page: 1, totalItems: 0 });
    });

    it('cancels an abandoned detail read and requests evidence for the replacement revision', async () => {
        const previousResult = vi.fn();
        const previous = facade.get(firstRevision).subscribe(previousResult);
        const abandoned = http.expectOne({ method: 'GET', url: `${queueUrl}/${firstRevision}` });
        previous.unsubscribe();
        expect(abandoned.cancelled).toBe(true);

        const result = firstValueFrom(facade.get(secondRevision));
        http.expectOne({ method: 'GET', url: `${queueUrl}/${secondRevision}` }).flush(selectedItem);
        const detail = await result;
        expect(detail).toMatchObject({ revisionId: secondRevision, summary: 'Vegetable stew', images: selectedItem.images });
        expect(previousResult).not.toHaveBeenCalled();

        const preview = firstValueFrom(facade.getImage(detail.revisionId, detail.images[0].assetId));
        const payload = { error: 'RecipeModeration.ImageNotFound', message: 'The checked image is no longer available.' };
        http.expectOne({ method: 'GET', url: `${queueUrl}/${secondRevision}/images/${asset}` }).flush(payload, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });
        await expect(preview).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: payload });
    });
});

describe('Admin recipe moderation actions', () => {
    it.each([
        { action: 'approve', reason: ' \n ', payload: { approve: true, reason: null } },
        { action: 'requestChanges', reason: '  Explain the next step.\n', payload: { approve: false, reason: 'Explain the next step.' } },
    ] as const)('resolves the selected revision for $action using the normalized review note', async ({ action, reason, payload }) => {
        const result = firstValueFrom(facade.applyAction(secondRevision, action, reason));
        const resolve = http.expectOne({ method: 'POST', url: `${queueUrl}/${secondRevision}/resolve` });
        expect(resolve.request.body).toEqual(payload);
        resolve.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

        await expect(result).resolves.toBeNull();
    });

    it('keeps a stale review distinct from a subsequent retry on another revision', async () => {
        const result = firstValueFrom(facade.applyAction(firstRevision, 'approve', 'Reviewed manually.'));
        const payload = { error: 'RecipeModeration.Conflict', message: 'The submission has changed. Reload it before trying again.' };
        http.expectOne({ method: 'POST', url: `${queueUrl}/${firstRevision}/resolve` }).flush(payload, {
            status: HttpStatusCode.Conflict,
            statusText: 'Conflict',
        });
        await expect(result).rejects.toMatchObject({ status: HttpStatusCode.Conflict, error: payload });

        const retry = firstValueFrom(facade.applyAction(secondRevision, 'retry', 'Old review note'));
        const request = http.expectOne({ method: 'POST', url: `${queueUrl}/${secondRevision}/retry` });
        expect(request.request.body).toBeNull();
        request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        await expect(retry).resolves.toBeNull();
    });
});

describe('Admin recipe moderation rules', () => {
    it('loads the active policy, historical rules and shadow provider capability', async () => {
        const result = firstValueFrom(facade.getPolicy());
        http.expectOne({ method: 'GET', url: rulesUrl }).flush(initialPolicy);

        await expect(result).resolves.toEqual({
            ...initialPolicy,
            history: [initialVersion, { ...historicalVersion, publishedBy: null }],
        });
    });

    it('publishes rules with an explicit zero sample percentage and restores an identified historical version', async () => {
        const published = firstValueFrom(facade.publishPolicy(publishedRules));
        const request = http.expectOne({ method: 'PUT', url: rulesUrl });
        expect(request.request.body).toEqual(publishedRules);
        const publishedVersion = { id: newPolicy, publishedAt: '2026-11-21T12:30:00Z', publishedBy, rules: publishedRules };
        const response = { ...initialPolicy, active: publishedVersion, history: [publishedVersion, ...initialPolicy.history] };
        request.flush(response);
        await expect(published).resolves.toMatchObject({ active: { id: newPolicy, rules: publishedRules } });

        const restored = firstValueFrom(facade.restorePolicy(historicalPolicy));
        const restore = http.expectOne({ method: 'POST', url: `${rulesUrl}/${historicalPolicy}/restore` });
        expect(restore.request.body).toBeNull();
        const restoredVersion = { id: restoredPolicy, publishedAt: '2026-11-22T12:30:00Z', publishedBy, rules: historicalRules };
        restore.flush({ ...response, active: restoredVersion, history: [restoredVersion, ...response.history] });
        const policy = await restored;
        expect(policy.active).toEqual(restoredVersion);
        expect(policy.history.map(version => version.id)).toEqual([restoredPolicy, newPolicy, activePolicy, historicalPolicy]);
    });
});

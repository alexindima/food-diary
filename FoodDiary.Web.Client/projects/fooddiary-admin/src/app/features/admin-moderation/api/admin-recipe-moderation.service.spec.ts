import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { AdminRecipeModerationImageHttpResponse } from '../../../shared/api/sdk/generated/model/admin-recipe-moderation-image-http-response';
import type { RecipeModerationPage } from '../../../shared/api/sdk/generated/model/recipe-moderation-page';
import { adminId } from '../../../shared/models/semantics/admin-meaning';
import { AdminRecipeModerationService } from './admin-recipe-moderation.service';

const queueUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation/recipes`;
const revision = adminId<'recipe-moderation-revision'>('a0000000-0000-4000-8000-000000000001');
const asset = adminId<'image-asset'>('a0000000-0000-4000-8000-000000000002');
const imageUrl = `${queueUrl}/${revision}/images/${asset}`;
const page = {
    page: 2,
    limit: 20,
    totalItems: 24,
    pendingCount: 5,
    needsReviewCount: 24,
    delayedCount: 2,
    items: [
        {
            revisionId: revision,
            recipeId: 'a0000000-0000-4000-8000-000000000003',
            ownerId: 'a0000000-0000-4000-8000-000000000004',
            status: 'NeedsReview',
            text: 'Roasted carrots\nCheck the ingredient notes.',
            createdAt: '2026-11-20T15:30:00Z',
            policyVersionId: 'a0000000-0000-4000-8000-000000000005',
            attempts: 1,
        },
    ],
} satisfies RecipeModerationPage;

describe('Admin recipe moderation HTTP', () => {
    let service: AdminRecipeModerationService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [AdminRecipeModerationService, provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(AdminRecipeModerationService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
    });

    it('sends queue pagination and status and keeps the separate moderation counts', async () => {
        const result = firstValueFrom(service.list(page.page, page.limit, 'NeedsReview'));
        const request = http.expectOne(req => req.url === queueUrl && req.method === 'GET');
        expect(Object.fromEntries(request.request.params.keys().map(key => [key, request.request.params.get(key)]))).toEqual({
            page: '2',
            limit: '20',
            status: 'NeedsReview',
        });
        request.flush(page);

        await expect(result).resolves.toEqual({
            ...page,
            items: [
                {
                    ...page.items[0],
                    summary: 'Roasted carrots',
                    updatedAt: null,
                    reason: null,
                    primaryResult: null,
                    shadowResult: null,
                    images: [],
                },
            ],
        });
    });

    it('requests private image evidence by both identifiers and retains its data URL', async () => {
        const response = {
            dataUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB',
        } satisfies AdminRecipeModerationImageHttpResponse;
        const result = firstValueFrom(service.getImage(revision, asset));
        http.expectOne({ method: 'GET', url: imageUrl }).flush(response);

        await expect(result).resolves.toBe(response.dataUrl);
    });

    it('rejects a successful image response missing its required evidence URL', async () => {
        const response: AdminRecipeModerationImageHttpResponse = {};
        const result = firstValueFrom(service.getImage(revision, asset));
        http.expectOne({ method: 'GET', url: imageUrl }).flush(response);

        await expect(result).rejects.toThrow('API response is missing required field: dataUrl.');
    });

    it('preserves an unavailable queue error without returning an empty page', async () => {
        const payload = { error: 'RecipeModeration.Unavailable', message: 'Try again later.' };
        const result = firstValueFrom(service.list(1, page.limit));
        http.expectOne(req => req.url === queueUrl && req.method === 'GET').flush(payload, {
            status: HttpStatusCode.ServiceUnavailable,
            statusText: 'Service Unavailable',
        });

        await expect(result).rejects.toMatchObject({ status: HttpStatusCode.ServiceUnavailable, error: payload });
    });
});

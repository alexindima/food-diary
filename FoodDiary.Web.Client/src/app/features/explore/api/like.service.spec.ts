import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { ApiErrorHttpResponse } from '../../../shared/api/sdk/generated/model/api-error-http-response';
import type { RecipeLikeStatus } from '../models/like.data';
import { LikeService } from './like.service';

const BASE_URL = environment.apiUrls.recipes;
const TOTAL_LIKES = 3;

let service: LikeService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [LikeService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LikeService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('LikeService', () => {
    it('gets like status', () => {
        const status = createStatus();

        service.getStatus('recipe-1').subscribe(result => {
            expect(result).toEqual(status);
        });

        const req = httpMock.expectOne(`${BASE_URL}/recipe-1/likes`);
        expect(req.request.method).toBe('GET');
        req.flush(status);
    });

    it('returns default status on getStatus failure', () => {
        service.getStatus('recipe-1').subscribe(result => {
            expect(result).toEqual({ isLiked: false, totalLikes: 0 });
        });

        const req = httpMock.expectOne(`${BASE_URL}/recipe-1/likes`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Server Error' });
    });

    it('toggles like status', () => {
        const status = createStatus({ isLiked: false });

        service.toggle('recipe-1', false).subscribe(result => {
            expect(result).toEqual(status);
        });

        const req = httpMock.expectOne(`${BASE_URL}/recipe-1/likes/toggle`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ isLiked: false });
        expect(req.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        req.flush(status);
    });

    it('propagates a recipe that became unavailable while a like was being saved', async () => {
        const recipeId = '8578ab03-f740-4b1a-b069-de389cb8c30b';
        const pending = firstValueFrom(service.toggle(recipeId, true));
        const removed = { error: 'Recipe.NotFound' } satisfies ApiErrorHttpResponse;
        const request = httpMock.expectOne({ method: 'POST', url: `${BASE_URL}/${recipeId}/likes/toggle` });
        expect(request.request.body).toEqual({ isLiked: true });
        expect(request.request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/u);
        request.flush(removed, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error: removed });
    });
});

function createStatus(overrides: Partial<RecipeLikeStatus> = {}): RecipeLikeStatus {
    return {
        isLiked: true,
        totalLikes: TOTAL_LIKES,
        ...overrides,
    };
}

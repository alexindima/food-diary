import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { DietologistRecommendation } from '../../../shared/models/dietologist.data';
import { RecommendationsService } from './recommendations.service';

const BASE_URL = environment.apiUrls.recommendations;

let service: RecommendationsService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [RecommendationsService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(RecommendationsService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('RecommendationsService', () => {
    it('gets current user recommendations', () => {
        const recommendations = [createRecommendation()];

        service.getMyRecommendations().subscribe(result => {
            expect(result).toEqual(recommendations);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush(recommendations);
    });

    it('marks recommendation as read', () => {
        service.markAsRead('recommendation-1').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/recommendation-1/read`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({});
        req.flush(null);
    });
});

function createRecommendation(): DietologistRecommendation {
    return {
        id: 'recommendation-1',
        dietologistUserId: 'dietologist-1',
        dietologistFirstName: 'Ada',
        dietologistLastName: 'Lovelace',
        text: 'Add a protein source to breakfast.',
        isRead: false,
        createdAtUtc: '2026-05-01T10:00:00.000Z',
        readAtUtc: null,
    };
}

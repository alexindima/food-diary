import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { GamificationData } from '../models/gamification.data';
import { GamificationService } from './gamification.service';

const BASE_URL = environment.apiUrls.gamification;
const MOCK_DATA: GamificationData = {
    currentStreak: 4,
    longestStreak: 10,
    totalMealsLogged: 38,
    healthScore: 76,
    weeklyAdherence: 0.88,
    badges: [{ key: 'streak_3', category: 'streak', threshold: 3, isEarned: true }],
};

describe('GamificationService', () => {
    let service: GamificationService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                GamificationService,
                { provide: TranslateService, useValue: { getCurrentLang: (): string => 'ru' } },
                provideHttpClient(),
                provideHttpClientTesting(),
            ],
        });

        service = TestBed.inject(GamificationService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('gets gamification data', () => {
        service.getData().subscribe(result => {
            expect(result).toEqual(MOCK_DATA);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        expect(req.request.headers.get('Accept-Language')).toBe('ru');
        req.flush(MOCK_DATA);
    });

    it('propagates request errors instead of inventing empty achievements', () => {
        let failed = false;
        service.getData().subscribe({
            next: () => expect.fail('Failed requests must not return achievement data'),
            error: (error: unknown) => {
                failed = true;
                expect(error).toBeInstanceOf(HttpErrorResponse);
                if (error instanceof HttpErrorResponse) {
                    expect(error.status).toBe(HttpStatusCode.InternalServerError);
                }
            },
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
        expect(failed).toBe(true);
    });
});

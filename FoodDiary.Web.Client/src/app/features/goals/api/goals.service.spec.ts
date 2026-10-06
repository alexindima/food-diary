import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { UserService } from '../../../shared/api/user.service';
import type { GoalsResponse, UpdateGoalsRequest } from '../../../shared/models/goals.data';
import { GoalsService } from './goals.service';

const UPDATED_CALORIE_TARGET = 2500;

describe('GoalsService', () => {
    let service: GoalsService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.goals;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [GoalsService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(GoalsService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should get goals', () => {
        const mockGoals: GoalsResponse = {
            dailyCalorieTarget: 2000,
            proteinTarget: 150,
            calorieCyclingEnabled: false,
        };

        service.getGoals().subscribe(result => {
            expect(result).toEqual(mockGoals);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        req.flush(mockGoals);
    });

    it('should return null on getGoals error', () => {
        service.getGoals().subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should rethrow strict getGoals errors', () => {
        const errorSpy = vi.fn();

        service.getGoalsStrict().subscribe({ error: errorSpy });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });

        expect(errorSpy).toHaveBeenCalledTimes(1);
    });

    it('should update goals', () => {
        const request: UpdateGoalsRequest = {
            dailyCalorieTarget: 2500,
            proteinTarget: 180,
        };
        const mockResponse: GoalsResponse = {
            dailyCalorieTarget: 2500,
            proteinTarget: 180,
            calorieCyclingEnabled: false,
        };

        service.updateGoals(request).subscribe(result => {
            expect(result).toEqual(mockResponse);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
        const profile = httpMock.expectOne(`${environment.apiUrls.users}/info`);
        profile.flush({ id: 'user-1', dailyCalorieTarget: 2500, calories: 2500 });
        expect(TestBed.inject(UserService).user()?.dailyCalorieTarget).toBe(UPDATED_CALORIE_TARGET);
        expect(TestBed.inject(UserService).user()?.calories).toBe(UPDATED_CALORIE_TARGET);
    });

    it('should return null on updateGoals error', () => {
        const request: UpdateGoalsRequest = { dailyCalorieTarget: 2500 };

        service.updateGoals(request).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

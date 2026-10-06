import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { UserService } from '../../../shared/api/user.service';
import type { GoalsResponse, UpdateGoalsRequest } from '../../../shared/models/goals.data';
import { GoalsService } from './goals.service';

const UPDATED_CALORIE_TARGET = 2500;

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

describe('GoalsService', () => {
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

describe('GoalsService body target updates', () => {
    it('clears explicit null targets sequentially after PATCH and publishes success only after refresh', () => {
        const next = vi.fn();
        service.updateGoals({ desiredWeightKg: null, desiredWaistCm: null }).subscribe(next);
        httpMock.expectNone(`${environment.apiUrls.users}/desired-weight`);
        httpMock.expectOne(`${baseUrl}/`).flush({ calorieCyclingEnabled: false, desiredWeightKg: 72, desiredWaistCm: 78 });
        const weight = httpMock.expectOne(`${environment.apiUrls.users}/desired-weight`);
        expect(weight.request.body).toEqual({ desiredWeightKg: null });
        httpMock.expectNone(`${environment.apiUrls.users}/desired-waist`);
        expect(next).not.toHaveBeenCalled();
        weight.flush({ desiredWeightKg: null });
        const waist = httpMock.expectOne(`${environment.apiUrls.users}/desired-waist`);
        expect(waist.request.body).toEqual({ desiredWaistCm: null });
        waist.flush({ desiredWaistCm: null });
        expect(next).not.toHaveBeenCalled();
        httpMock.expectOne(`${environment.apiUrls.users}/info`).flush({ id: 'user-1' });
        expect(next).toHaveBeenCalledExactlyOnceWith({
            calorieCyclingEnabled: false,
            desiredWeightKg: null,
            desiredWaistCm: null,
        });
    });

    it('preserves positive targets and undefined no-op without using clear endpoints', () => {
        const response: GoalsResponse = { calorieCyclingEnabled: false, desiredWeightKg: 72, desiredWaistCm: 78 };
        const next = vi.fn();
        service.updateGoals({ desiredWeightKg: 72 }).subscribe(next);
        httpMock.expectOne(`${baseUrl}/`).flush(response);
        httpMock.expectNone(`${environment.apiUrls.users}/desired-weight`);
        httpMock.expectNone(`${environment.apiUrls.users}/desired-waist`);
        httpMock.expectOne(`${environment.apiUrls.users}/info`).flush({ id: 'user-1' });
        expect(next).toHaveBeenCalledExactlyOnceWith(response);
    });

    it('skips redundant clears for explicit null responses but clears targets omitted from the response', () => {
        const next = vi.fn();
        service.updateGoals({ desiredWeightKg: null, desiredWaistCm: null }).subscribe(next);
        httpMock.expectOne(`${baseUrl}/`).flush({ calorieCyclingEnabled: false, desiredWeightKg: null, desiredWaistCm: null });
        httpMock.expectNone(`${environment.apiUrls.users}/desired-weight`);
        httpMock.expectNone(`${environment.apiUrls.users}/desired-waist`);
        httpMock.expectOne(`${environment.apiUrls.users}/info`).flush({ id: 'user-1' });
        expect(next).toHaveBeenCalledTimes(1);

        service.updateGoals({ desiredWeightKg: null, desiredWaistCm: null }).subscribe(next);
        httpMock.expectOne(`${baseUrl}/`).flush({ calorieCyclingEnabled: false });
        httpMock.expectOne(`${environment.apiUrls.users}/desired-weight`).flush({ desiredWeightKg: null });
        httpMock.expectOne(`${environment.apiUrls.users}/desired-waist`).flush({ desiredWaistCm: null });
        httpMock.expectOne(`${environment.apiUrls.users}/info`).flush({ id: 'user-1' });
        expect(next).toHaveBeenCalledTimes(2);
    });
});

describe('GoalsService failed body target updates', () => {
    it('does not clear targets after a rejected PATCH', () => {
        const next = vi.fn();
        service.updateGoals({ desiredWeightKg: null, desiredWaistCm: null }).subscribe(next);
        httpMock.expectOne(`${baseUrl}/`).flush('Invalid goals', { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });
        httpMock.expectNone(`${environment.apiUrls.users}/desired-weight`);
        httpMock.expectNone(`${environment.apiUrls.users}/desired-waist`);
        httpMock.expectNone(`${environment.apiUrls.users}/info`);
        expect(next).toHaveBeenCalledExactlyOnceWith(null);
    });

    it('propagates clear failure without a success value or profile refresh and allows retry', () => {
        const request: UpdateGoalsRequest = { desiredWeightKg: null, desiredWaistCm: null };
        const next = vi.fn();
        const error = vi.fn();
        const response: GoalsResponse = { calorieCyclingEnabled: false, desiredWeightKg: 72, desiredWaistCm: 78 };
        service.updateGoals(request).subscribe({ next, error });
        httpMock.expectOne(`${baseUrl}/`).flush(response);
        httpMock.expectOne(`${environment.apiUrls.users}/desired-weight`).flush('Unavailable', {
            status: HttpStatusCode.InternalServerError,
            statusText: 'Internal Server Error',
        });
        httpMock.expectNone(`${environment.apiUrls.users}/desired-waist`);
        httpMock.expectNone(`${environment.apiUrls.users}/info`);
        expect(next).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledTimes(1);

        service.updateGoals(request).subscribe(next);
        httpMock.expectOne(`${baseUrl}/`).flush(response);
        httpMock.expectOne(`${environment.apiUrls.users}/desired-weight`).flush({ desiredWeightKg: null });
        httpMock.expectOne(`${environment.apiUrls.users}/desired-waist`).flush({ desiredWaistCm: null });
        httpMock.expectOne(`${environment.apiUrls.users}/info`).flush({ id: 'user-1' });
        expect(next).toHaveBeenCalledTimes(1);
    });
});

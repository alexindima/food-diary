import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { WeeklyCheckInData } from '../models/weekly-check-in.data';
import { WeeklyCheckInService } from './weekly-check-in.service';

const BASE_URL = environment.apiUrls.weeklyCheckIn;
const MOCK_DATA: WeeklyCheckInData = {
    thisWeek: {
        totalCalories: 14000,
        avgDailyCalories: 2000,
        avgProteins: 110,
        avgFats: 70,
        avgCarbs: 210,
        mealsLogged: 21,
        daysLogged: 7,
        weightStart: 74,
        weightEnd: 73.5,
        waistStart: 82,
        waistEnd: 81,
        totalHydrationMl: 14000,
        avgDailyHydrationMl: 2000,
    },
    lastWeek: {
        totalCalories: 15000,
        avgDailyCalories: 2142,
        avgProteins: 100,
        avgFats: 75,
        avgCarbs: 230,
        mealsLogged: 18,
        daysLogged: 6,
        weightStart: 75,
        weightEnd: 74,
        waistStart: 83,
        waistEnd: 82,
        totalHydrationMl: 12000,
        avgDailyHydrationMl: 1714,
    },
    trends: {
        calorieChange: -142,
        proteinChange: 10,
        fatChange: -5,
        carbChange: -20,
        weightChange: -0.5,
        waistChange: -1,
        hydrationChange: 286,
        mealsLoggedChange: 3,
    },
    suggestions: ['ADD_PROTEIN'],
};

let service: WeeklyCheckInService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [WeeklyCheckInService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(WeeklyCheckInService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('WeeklyCheckInService', () => {
    it('gets weekly check-in data', () => {
        service.getData().subscribe(data => {
            expect(data).toEqual(MOCK_DATA);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_DATA);
    });

    it('returns empty data on error', () => {
        service.getData().subscribe(data => {
            expect(data.thisWeek.totalCalories).toBe(0);
            expect(data.lastWeek.totalCalories).toBe(0);
            expect(data.trends.calorieChange).toBe(0);
            expect(data.trends.weightChange).toBeNull();
            expect(data.suggestions).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import {
    BLEEDING_TYPE_BLEEDING,
    type CreateCyclePayload,
    CYCLE_FLOW_MEDIUM,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CycleLogDay,
    type CycleResponse,
    type UpsertCycleDayPayload,
} from '../models/cycle.data';
import { CyclesService } from './cycles.service';

const BASE_URL = environment.apiUrls.cycles;
const MOCK_CYCLE: CycleResponse = {
    id: 'c-1',
    userId: 'user-1',
    mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    confidence: 1,
    trackingStartDate: '2026-03-01',
    averageCycleLength: 28,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: true,
    isOnboardingComplete: true,
    showFertilityEstimates: true,
    discreetNotifications: true,
    notes: null,
    bleedingEntries: [],
    symptoms: [],
    factors: [],
    fertilitySignals: [],
    predictions: null,
};
const MOCK_DAY: CycleLogDay = {
    cycleProfileId: 'c-1',
    date: '2026-03-05',
    bleedingEntries: [
        {
            id: 'b-1',
            cycleProfileId: 'c-1',
            date: '2026-03-05',
            type: BLEEDING_TYPE_BLEEDING,
            flow: CYCLE_FLOW_MEDIUM,
            painImpact: 2,
            notes: null,
        },
    ],
    symptoms: [],
    fertilitySignal: null,
};

let service: CyclesService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [CyclesService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(CyclesService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('CyclesService current cycle', () => {
    it('should get current cycle', () => {
        service.getCurrent().subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_CYCLE);
    });

    it('should return null on getCurrent error', () => {
        service.getCurrent().subscribe(cycle => {
            expect(cycle).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/current`);
        req.flush('Not found', { status: 404, statusText: 'Not Found' });
    });
});

describe('CyclesService mutations', () => {
    it('should create cycle', () => {
        const payload: CreateCyclePayload = {
            trackingStartDate: '2026-03-01',
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: 28,
            averagePeriodLength: 5,
            lutealLength: 14,
            isRegular: true,
            isOnboardingComplete: true,
            showFertilityEstimates: true,
            discreetNotifications: true,
            notes: null,
        };

        service.create(payload).subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_CYCLE);
    });

    it('should upsert cycle day', () => {
        const payload: UpsertCycleDayPayload = {
            date: '2026-03-05',
            bleeding: {
                type: BLEEDING_TYPE_BLEEDING,
                flow: CYCLE_FLOW_MEDIUM,
                painImpact: 2,
                notes: null,
                clearNotes: false,
            },
            symptoms: [],
            fertilitySignal: null,
        };

        service.upsertDay('c-1', payload).subscribe(day => {
            expect(day).toEqual(MOCK_DAY);
        });

        const req = httpMock.expectOne(`${BASE_URL}/c-1/days`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_DAY);
    });
});

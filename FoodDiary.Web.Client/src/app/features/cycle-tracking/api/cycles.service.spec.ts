import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { CycleDay, CycleResponse } from '../models/cycle.data';
import { CyclesService } from './cycles.service';

const BASE_URL = environment.apiUrls.cycles;
const MOCK_CYCLE: CycleResponse = {
    id: 'c-1',
    userId: 'user-1',
    startDate: '2026-03-01',
    averageLength: 28,
    lutealLength: 14,
    notes: null,
    days: [],
    predictions: null,
};
const MOCK_DAY: CycleDay = {
    id: 'd-1',
    cycleId: 'c-1',
    date: '2026-03-05',
    isPeriod: true,
    symptoms: {
        pain: 2,
        mood: 3,
        edema: 0,
        headache: 1,
        energy: 3,
        sleepQuality: 4,
        libido: 2,
    },
    notes: null,
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
        const payload = { startDate: '2026-03-01', averageLength: 28, lutealLength: 14, notes: null };

        service.create(payload).subscribe(cycle => {
            expect(cycle).toEqual(MOCK_CYCLE);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_CYCLE);
    });

    it('should upsert cycle day', () => {
        const payload = {
            date: '2026-03-05',
            isPeriod: true,
            symptoms: {
                pain: 2,
                mood: 3,
                edema: 0,
                headache: 1,
                energy: 3,
                sleepQuality: 4,
                libido: 2,
            },
            notes: null,
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

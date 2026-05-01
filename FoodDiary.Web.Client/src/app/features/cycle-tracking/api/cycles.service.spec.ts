import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { CycleDay, CycleResponse } from '../models/cycle.data';
import { CyclesService } from './cycles.service';

describe('CyclesService', () => {
    let service: CyclesService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.cycles;

    const mockCycle: CycleResponse = {
        id: 'c-1',
        userId: 'user-1',
        startDate: '2026-03-01',
        averageLength: 28,
        lutealLength: 14,
        notes: null,
        days: [],
        predictions: null,
    };

    const mockDay: CycleDay = {
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

    it('should get current cycle', () => {
        service.getCurrent().subscribe(cycle => {
            expect(cycle).toEqual(mockCycle);
        });

        const req = httpMock.expectOne(`${baseUrl}/current`);
        expect(req.request.method).toBe('GET');
        req.flush(mockCycle);
    });

    it('should return null on getCurrent error', () => {
        service.getCurrent().subscribe(cycle => {
            expect(cycle).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/current`);
        req.flush('Not found', { status: 404, statusText: 'Not Found' });
    });

    it('should create cycle', () => {
        const payload = { startDate: '2026-03-01', averageLength: 28, lutealLength: 14, notes: null };

        service.create(payload).subscribe(cycle => {
            expect(cycle).toEqual(mockCycle);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(mockCycle);
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
            expect(day).toEqual(mockDay);
        });

        const req = httpMock.expectOne(`${baseUrl}/c-1/days`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(mockDay);
    });
});

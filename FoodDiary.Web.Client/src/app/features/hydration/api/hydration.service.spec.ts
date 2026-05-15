import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { HydrationDaily, HydrationEntry } from '../models/hydration.data';
import { HydrationService } from './hydration.service';

const ENTRY_AMOUNT_ML = 250;
const BASE_URL = environment.apiUrls.hydration;
const TEST_DATE = new Date('2026-03-28T12:00:00.000Z');
const MOCK_DAILY: HydrationDaily = {
    dateUtc: '2026-03-28T00:00:00.000Z',
    totalMl: 1500,
    goalMl: 2500,
};
const MOCK_ENTRY: HydrationEntry = {
    id: 'h-1',
    timestampUtc: '2026-03-28T12:00:00.000Z',
    amountMl: ENTRY_AMOUNT_ML,
};

describe('HydrationService', () => {
    let service: HydrationService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [HydrationService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(HydrationService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should get daily hydration', () => {
        service.getDaily(TEST_DATE).subscribe(daily => {
            expect(daily).toEqual(MOCK_DAILY);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/daily`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateUtc')).toBe(TEST_DATE.toISOString());
        req.flush(MOCK_DAILY);
    });

    it('should return default daily on error', () => {
        service.getDaily(TEST_DATE).subscribe(daily => {
            expect(daily.totalMl).toBe(0);
            expect(daily.goalMl).toBeNull();
            expect(daily.dateUtc).toBe(TEST_DATE.toISOString());
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/daily`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should get entries for date', () => {
        service.getEntries(TEST_DATE).subscribe(entries => {
            expect(entries).toEqual([MOCK_ENTRY]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateUtc')).toBe(TEST_DATE.toISOString());
        req.flush([MOCK_ENTRY]);
    });

    it('should return empty array on entries error', () => {
        service.getEntries(TEST_DATE).subscribe(entries => {
            expect(entries).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should add entry', () => {
        const timestamp = new Date('2026-03-28T14:30:00.000Z');

        service.addEntry(ENTRY_AMOUNT_ML, timestamp).subscribe(entry => {
            expect(entry).toEqual(MOCK_ENTRY);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({
            amountMl: ENTRY_AMOUNT_ML,
            timestampUtc: timestamp.toISOString(),
        });
        req.flush(MOCK_ENTRY);
    });

    it('should rethrow add entry errors', () => {
        const timestamp = new Date('2026-03-28T14:30:00.000Z');
        let errorStatus: number | null = null;

        service.addEntry(ENTRY_AMOUNT_ML, timestamp).subscribe({
            error: (error: unknown) => {
                errorStatus = error instanceof Error ? null : getStatus(error);
            },
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server error', { status: HttpStatusCode.BadRequest, statusText: 'Bad Request' });

        expect(errorStatus).toBe(HttpStatusCode.BadRequest);
    });
});

function getStatus(error: unknown): number | null {
    if (typeof error !== 'object' || error === null || !('status' in error)) {
        return null;
    }

    const status = error.status;
    return typeof status === 'number' ? status : null;
}

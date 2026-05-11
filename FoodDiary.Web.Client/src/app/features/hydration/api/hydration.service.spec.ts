import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { HydrationDaily, HydrationEntry } from '../models/hydration.data';
import { HydrationService } from './hydration.service';

const ENTRY_AMOUNT_ML = 250;
const HTTP_INTERNAL_SERVER_ERROR = 500;

describe('HydrationService', () => {
    let service: HydrationService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.hydration;

    const testDate = new Date('2026-03-28T12:00:00.000Z');

    const mockDaily: HydrationDaily = {
        dateUtc: '2026-03-28T00:00:00.000Z',
        totalMl: 1500,
        goalMl: 2500,
    };

    const mockEntry: HydrationEntry = {
        id: 'h-1',
        timestampUtc: '2026-03-28T12:00:00.000Z',
        amountMl: ENTRY_AMOUNT_ML,
    };

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
        service.getDaily(testDate).subscribe(daily => {
            expect(daily).toEqual(mockDaily);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/daily`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateUtc')).toBe(testDate.toISOString());
        req.flush(mockDaily);
    });

    it('should return default daily on error', () => {
        service.getDaily(testDate).subscribe(daily => {
            expect(daily.totalMl).toBe(0);
            expect(daily.goalMl).toBeNull();
            expect(daily.dateUtc).toBe(testDate.toISOString());
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/daily`);
        req.flush('Server error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });

    it('should get entries for date', () => {
        service.getEntries(testDate).subscribe(entries => {
            expect(entries).toEqual([mockEntry]);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateUtc')).toBe(testDate.toISOString());
        req.flush([mockEntry]);
    });

    it('should return empty array on entries error', () => {
        service.getEntries(testDate).subscribe(entries => {
            expect(entries).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/`);
        req.flush('Server error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });

    it('should add entry', () => {
        const timestamp = new Date('2026-03-28T14:30:00.000Z');

        service.addEntry(ENTRY_AMOUNT_ML, timestamp).subscribe(entry => {
            expect(entry).toEqual(mockEntry);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({
            amountMl: ENTRY_AMOUNT_ML,
            timestampUtc: timestamp.toISOString(),
        });
        req.flush(mockEntry);
    });
});

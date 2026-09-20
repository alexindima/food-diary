import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { WeightEntry, WeightEntryFilters } from '../models/weight-entry.data';
import { WeightEntriesService } from './weight-entries.service';

const BASE_URL = environment.apiUrls.weights;
const ENTRY_LIMIT = 10;
const MOCK_ENTRY: WeightEntry = {
    id: 'w-1',
    userId: 'user-1',
    date: '2026-03-01',
    weightKg: 75.5,
};

let service: WeightEntriesService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [WeightEntriesService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(WeightEntriesService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('WeightEntriesService list', () => {
    it('should get entries', () => {
        service.getEntries().subscribe(entries => {
            expect(entries).toEqual([MOCK_ENTRY]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush([MOCK_ENTRY]);
    });

    it('should get entries with filters', () => {
        const filters: WeightEntryFilters = { dateFrom: '2026-01-01', dateTo: '2026-03-01', limit: ENTRY_LIMIT, sort: 'desc' };

        service.getEntries(filters).subscribe(entries => {
            expect(entries).toEqual([MOCK_ENTRY]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateFrom')).toBe('2026-01-01');
        expect(req.request.params.get('dateTo')).toBe('2026-03-01');
        expect(req.request.params.get('limit')).toBe(`${ENTRY_LIMIT}`);
        expect(req.request.params.get('sort')).toBe('desc');
        req.flush([MOCK_ENTRY]);
    });

    it('should skip empty optional filters', () => {
        service.getEntries({ dateFrom: '', dateTo: undefined, limit: undefined }).subscribe(entries => {
            expect(entries).toEqual([MOCK_ENTRY]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/`);
        expect(req.request.params.keys()).toEqual([]);
        req.flush([MOCK_ENTRY]);
    });

    it('should return empty array on getEntries error', () => {
        service.getEntries().subscribe(entries => {
            expect(entries).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('WeightEntriesService latest', () => {
    it('should get latest entry', () => {
        service.getLatest().subscribe(entry => {
            expect(entry).toEqual(MOCK_ENTRY);
        });

        const req = httpMock.expectOne(`${BASE_URL}/latest`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_ENTRY);
    });

    it('should return null on getLatest error', () => {
        service.getLatest().subscribe(entry => {
            expect(entry).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/latest`);
        req.flush('Not found', { status: 404, statusText: 'Not Found' });
    });
});

describe('WeightEntriesService mutations', () => {
    it('should create entry', () => {
        const payload = { date: '2026-03-28', weightKg: 76 };

        service.create(payload).subscribe(entry => {
            expect(entry).toEqual(MOCK_ENTRY);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_ENTRY);
    });

    it('should update entry', () => {
        const payload = { date: '2026-03-28', weightKg: 77 };
        const updated = { ...MOCK_ENTRY, weightKg: 77 };

        service.update('w-1', payload).subscribe(entry => {
            expect(entry).toEqual(updated);
        });

        const req = httpMock.expectOne(`${BASE_URL}/w-1`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(updated);
    });

    it('should remove entry', () => {
        service.remove('w-1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/w-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

describe('Summary and failure contracts', () => {
    const filters = { dateFrom: '2026-01-01', dateTo: '2026-02-01', quantizationDays: 1 };
    it('sends summary parameters and returns buckets', () => {
        const received = vi.fn();
        service.getSummary(filters).subscribe(received);
        const request = httpMock.expectOne(r => r.url === `${BASE_URL}/summary`);
        expect(request.request.method).toBe('GET');
        expect(Object.fromEntries(request.request.params.keys().map(key => [key, request.request.params.get(key)]))).toEqual({
            ...filters,
            quantizationDays: '1',
        });
        const buckets = [{ startDate: filters.dateFrom, endDate: filters.dateTo, averageWeightKg: 75 }];
        request.flush(buckets);
        expect(received).toHaveBeenCalledWith(buckets);
    });
    it('falls back to empty summary after a server error', () => {
        const received = vi.fn();
        service.getSummary(filters).subscribe(received);
        httpMock.expectOne(r => r.url === `${BASE_URL}/summary`).flush({}, { status: 500, statusText: 'Failure' });
        expect(received).toHaveBeenCalledWith([]);
    });
    it('preserves page data and independent recent-entry limit', () => {
        const received = vi.fn();
        service.getPageSummary({ ...filters, entriesLimit: 500 }).subscribe(received);
        const request = httpMock.expectOne(r => r.url === `${BASE_URL}/page-summary`);
        expect(request.request.params.get('entriesLimit')).toBe('500');
        expect(request.request.params.get('dateFrom')).toBe(filters.dateFrom);
        expect(request.request.params.get('dateTo')).toBe(filters.dateTo);
        expect(request.request.params.get('quantizationDays')).toBe('1');
        const page = {
            entries: [MOCK_ENTRY],
            summary: [],
            heightCm: null,
            goal: { desiredWeightKg: null, startWeightKg: null, startedAtUtc: null },
            goalHistory: [],
        };
        request.flush(page);
        expect(received).toHaveBeenCalledWith(page);
    });
    it.each(['create', 'update', 'remove', 'page'] as const)('propagates %s failures', operation => {
        const next = vi.fn();
        const error = vi.fn();
        const payload = { date: '2026-04-01', weightKg: 75 };
        const response: Observable<unknown> =
            operation === 'create'
                ? service.create(payload)
                : operation === 'update'
                  ? service.update('id', payload)
                  : operation === 'remove'
                    ? service.remove('id')
                    : service.getPageSummary({ ...filters, entriesLimit: 10 });
        response.subscribe({ next, error });
        const body = { error: 'Metric.AlreadyExists' };
        httpMock.expectOne(() => true).flush(body, { status: 409, statusText: 'Conflict' });
        expect(next).not.toHaveBeenCalled();
        expect(error).toHaveBeenCalledWith(expect.objectContaining({ status: 409, error: body }));
    });
});

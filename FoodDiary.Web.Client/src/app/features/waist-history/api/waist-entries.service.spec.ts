import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { WaistEntry, WaistEntryFilters } from '../models/waist-entry.data';
import { WaistEntriesService } from './waist-entries.service';

const BASE_URL = environment.apiUrls.waists;
const ENTRY_LIMIT = 10;
const MOCK_ENTRY: WaistEntry = {
    id: 'wa-1',
    userId: 'user-1',
    date: '2026-03-01',
    circumference: 82.0,
};

let service: WaistEntriesService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [WaistEntriesService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(WaistEntriesService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('WaistEntriesService list', () => {
    it('should get entries', () => {
        service.getEntries().subscribe(entries => {
            expect(entries).toEqual([MOCK_ENTRY]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush([MOCK_ENTRY]);
    });

    it('should get entries with filters', () => {
        const filters: WaistEntryFilters = { dateFrom: '2026-01-01', dateTo: '2026-03-01', limit: ENTRY_LIMIT, sort: 'desc' };

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

describe('WaistEntriesService latest', () => {
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
        req.flush('Not found', { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    });
});

describe('WaistEntriesService mutations', () => {
    it('should create entry', () => {
        const payload = { date: '2026-03-28', circumference: 80.0 };

        service.create(payload).subscribe(entry => {
            expect(entry).toEqual(MOCK_ENTRY);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_ENTRY);
    });

    it('should update entry', () => {
        const payload = { date: '2026-03-28', circumference: 81.0 };
        const updated = { ...MOCK_ENTRY, circumference: 81.0 };

        service.update('wa-1', payload).subscribe(entry => {
            expect(entry).toEqual(updated);
        });

        const req = httpMock.expectOne(`${BASE_URL}/wa-1`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(updated);
    });

    it('should remove entry', () => {
        service.remove('wa-1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/wa-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { WeightEntry } from '../models/weight-entry.data';
import { WeightEntriesService } from './weight-entries.service';

const BASE_URL = environment.apiUrls.weights;
const MOCK_ENTRY: WeightEntry = {
    id: 'w-1',
    userId: 'user-1',
    date: '2026-03-01',
    weight: 75.5,
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

    it('should get entries with date filters', () => {
        const filters = { dateFrom: '2026-01-01', dateTo: '2026-03-01' };

        service.getEntries(filters).subscribe(entries => {
            expect(entries).toEqual([MOCK_ENTRY]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateFrom')).toBe('2026-01-01');
        expect(req.request.params.get('dateTo')).toBe('2026-03-01');
        req.flush([MOCK_ENTRY]);
    });

    it('should return empty array on getEntries error', () => {
        service.getEntries().subscribe(entries => {
            expect(entries).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
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
        const payload = { date: '2026-03-28', weight: 76.0 };

        service.create(payload).subscribe(entry => {
            expect(entry).toEqual(MOCK_ENTRY);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(MOCK_ENTRY);
    });

    it('should update entry', () => {
        const payload = { date: '2026-03-28', weight: 77.0 };
        const updated = { ...MOCK_ENTRY, weight: 77.0 };

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

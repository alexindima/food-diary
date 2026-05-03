import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { type WeightEntry } from '../models/weight-entry.data';
import { WeightEntriesService } from './weight-entries.service';

describe('WeightEntriesService', () => {
    let service: WeightEntriesService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.weights;

    const mockEntry: WeightEntry = {
        id: 'w-1',
        userId: 'user-1',
        date: '2026-03-01',
        weight: 75.5,
    };

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

    it('should get entries', () => {
        service.getEntries().subscribe(entries => {
            expect(entries).toEqual([mockEntry]);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        req.flush([mockEntry]);
    });

    it('should get entries with date filters', () => {
        const filters = { dateFrom: '2026-01-01', dateTo: '2026-03-01' };

        service.getEntries(filters).subscribe(entries => {
            expect(entries).toEqual([mockEntry]);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('dateFrom')).toBe('2026-01-01');
        expect(req.request.params.get('dateTo')).toBe('2026-03-01');
        req.flush([mockEntry]);
    });

    it('should return empty array on getEntries error', () => {
        service.getEntries().subscribe(entries => {
            expect(entries).toEqual([]);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should get latest entry', () => {
        service.getLatest().subscribe(entry => {
            expect(entry).toEqual(mockEntry);
        });

        const req = httpMock.expectOne(`${baseUrl}/latest`);
        expect(req.request.method).toBe('GET');
        req.flush(mockEntry);
    });

    it('should return null on getLatest error', () => {
        service.getLatest().subscribe(entry => {
            expect(entry).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/latest`);
        req.flush('Not found', { status: 404, statusText: 'Not Found' });
    });

    it('should create entry', () => {
        const payload = { date: '2026-03-28', weight: 76.0 };

        service.create(payload).subscribe(entry => {
            expect(entry).toEqual(mockEntry);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(mockEntry);
    });

    it('should update entry', () => {
        const payload = { date: '2026-03-28', weight: 77.0 };
        const updated = { ...mockEntry, weight: 77.0 };

        service.update('w-1', payload).subscribe(entry => {
            expect(entry).toEqual(updated);
        });

        const req = httpMock.expectOne(`${baseUrl}/w-1`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(updated);
    });

    it('should remove entry', () => {
        service.remove('w-1').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/w-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

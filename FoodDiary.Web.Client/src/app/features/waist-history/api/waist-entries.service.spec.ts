import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { WaistEntriesService } from './waist-entries.service';
import { environment } from '../../../../environments/environment';
import { WaistEntry } from '../models/waist-entry.data';

describe('WaistEntriesService', () => {
    let service: WaistEntriesService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.waists;

    const mockEntry: WaistEntry = {
        id: 'wa-1',
        userId: 'user-1',
        date: '2026-03-01',
        circumference: 82.0,
    };

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

    it('should get entries', () => {
        service.getEntries().subscribe(entries => {
            expect(entries).toEqual([mockEntry]);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('GET');
        req.flush([mockEntry]);
    });

    it('should return empty array on error', () => {
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

    it('should create entry', () => {
        const payload = { date: '2026-03-28', circumference: 80.0 };

        service.create(payload).subscribe(entry => {
            expect(entry).toEqual(mockEntry);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(mockEntry);
    });

    it('should update entry', () => {
        const payload = { date: '2026-03-28', circumference: 81.0 };
        const updated = { ...mockEntry, circumference: 81.0 };

        service.update('wa-1', payload).subscribe(entry => {
            expect(entry).toEqual(updated);
        });

        const req = httpMock.expectOne(`${baseUrl}/wa-1`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush(updated);
    });

    it('should remove entry', () => {
        service.remove('wa-1').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/wa-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

import { HttpHeaders, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { ExportService } from './export.service';

const BASE_URL = 'http://localhost:5300/api/v1/export';
const BLOB_URL = 'blob:food-diary';

let service: ExportService;
let httpMock: HttpTestingController;
let createObjectUrlSpy: ReturnType<typeof vi.spyOn>;
let revokeObjectUrlSpy: ReturnType<typeof vi.spyOn>;
let clickedDownloadName: string | null;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [ExportService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ExportService);
    httpMock = TestBed.inject(HttpTestingController);
    createObjectUrlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue(BLOB_URL);
    revokeObjectUrlSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    clickedDownloadName = null;
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
        clickedDownloadName = this.download;
    });
});

afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
});

describe('ExportService', () => {
    it('should request CSV export with defaults and download fallback filename', () => {
        service.exportDiary({ dateFrom: '2026-05-01', dateTo: '2026-05-14' }).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/diary` && r.method === 'GET');
        expect(req.request.params.get('dateFrom')).toBe('2026-05-01');
        expect(req.request.params.get('dateTo')).toBe('2026-05-14');
        expect(req.request.params.get('format')).toBe('csv');
        expect(req.request.params.get('reportOrigin')).toBe(window.location.origin);

        req.flush(new Blob(['csv'], { type: 'text/csv' }));

        expect(createObjectUrlSpy).toHaveBeenCalled();
        expect(clickedDownloadName).toBe('food-diary.csv');
        expect(revokeObjectUrlSpy).toHaveBeenCalledWith(BLOB_URL);
    });

    it('should use filename from content disposition and include PDF params', () => {
        service
            .exportDiary({
                dateFrom: '2026-05-01',
                dateTo: '2026-05-14',
                format: 'pdf',
                locale: 'ru',
                timeZoneOffsetMinutes: -240,
            })
            .subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/diary` && r.method === 'GET');
        expect(req.request.params.get('format')).toBe('pdf');
        expect(req.request.params.get('locale')).toBe('ru');
        expect(req.request.params.get('timeZoneOffsetMinutes')).toBe('-240');

        req.flush(new Blob(['pdf'], { type: 'application/pdf' }), {
            headers: new HttpHeaders({ 'Content-Disposition': "attachment; filename*=UTF-8''diary.pdf" }),
        });

        expect(clickedDownloadName).toBe('diary.pdf');
    });

    it('should skip download when response body is null', () => {
        service.exportDiary({ dateFrom: '2026-05-01', dateTo: '2026-05-14' }).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/diary` && r.method === 'GET');
        req.flush(null);

        expect(createObjectUrlSpy).not.toHaveBeenCalled();
        expect(clickedDownloadName).toBeNull();
    });
});

import { HttpHeaders, HttpResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowserWindowService } from '../platform/browser-window.service';
import { ExportService } from './export.service';

const BASE_URL = 'http://localhost:5300/api/v1/export';
const BLOB_URL = 'blob:food-diary';
const REPORT_ORIGIN = 'https://fooddiary.test';

let service: ExportService;
let httpMock: HttpTestingController;
let createObjectUrlSpy: ReturnType<typeof vi.spyOn>;
let revokeObjectUrlSpy: ReturnType<typeof vi.spyOn>;
let clickedDownloadName: string | null;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [
            ExportService,
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: BrowserWindowService, useValue: { getOrigin: (): string => REPORT_ORIGIN } },
        ],
    });
    service = TestBed.inject(ExportService);
    httpMock = TestBed.inject(HttpTestingController);
    createObjectUrlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue(BLOB_URL);
    revokeObjectUrlSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
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
    it('should request CSV diary export with defaults and download fallback filename', () => {
        service.exportDiary({ dateFrom: '2026-05-01', dateTo: '2026-05-14' }).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/diary` && r.method === 'GET');
        expect(req.request.params.get('dateFrom')).toBe('2026-05-01');
        expect(req.request.params.get('dateTo')).toBe('2026-05-14');
        expect(req.request.params.get('format')).toBe('csv');
        expect(req.request.params.get('reportOrigin')).toBe(REPORT_ORIGIN);

        req.flush(new Blob(['csv'], { type: 'text/csv' }));

        expect(createObjectUrlSpy).toHaveBeenCalled();
        expect(clickedDownloadName).toBe('food-diary.csv');
        expect(revokeObjectUrlSpy).toHaveBeenCalledWith(BLOB_URL);
    });

    it('should use filename from content disposition and include PDF diary params', () => {
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

    it('should request cycle export and use cycle fallback filename', () => {
        service
            .exportCycle({
                dateFrom: '2026-05-01T00:00:00.000Z',
                dateTo: '2026-05-14T23:59:59.999Z',
                timeZoneOffsetMinutes: 240,
            })
            .subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/cycle` && r.method === 'GET');
        expect(req.request.params.get('dateFrom')).toBe('2026-05-01T00:00:00.000Z');
        expect(req.request.params.get('dateTo')).toBe('2026-05-14T23:59:59.999Z');
        expect(req.request.params.get('timeZoneOffsetMinutes')).toBe('240');

        req.flush(new Blob(['csv'], { type: 'text/csv' }));

        expect(clickedDownloadName).toBe('cycle-tracking.csv');
    });

    it('should decode percent-encoded filename from content disposition', () => {
        service.exportDiary({ dateFrom: '2026-05-01', dateTo: '2026-05-14', format: 'pdf' }).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/diary` && r.method === 'GET');
        req.flush(new Blob(['pdf'], { type: 'application/pdf' }), {
            headers: new HttpHeaders({ 'Content-Disposition': "attachment; filename*=UTF-8''food%20diary.pdf" }),
        });

        expect(clickedDownloadName).toBe('food diary.pdf');
    });

    it('should skip download when response body is null', () => {
        service.exportDiary({ dateFrom: '2026-05-01', dateTo: '2026-05-14' }).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/diary` && r.method === 'GET');
        req.event(new HttpResponse<Blob>({ body: null }));

        expect(createObjectUrlSpy).not.toHaveBeenCalled();
        expect(clickedDownloadName).toBeNull();
    });
});

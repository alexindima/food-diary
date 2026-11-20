import { HttpHeaders, HttpResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { SessionEventsService } from '../auth/session-events.service';
import { BrowserWindowService } from '../platform/browser-window.service';
import { ExportService } from './export.service';

const BASE_URL = 'http://localhost:5300/api/v1/export';
const BLOB_URL = 'blob:food-diary';
const REPORT_ORIGIN = 'https://fooddiary.test';

it('cancels a personal archive before downloading it when the session ends', () => {
    let completed = false;
    service.exportPersonalData().subscribe({
        complete: () => {
            completed = true;
        },
    });
    const request = httpMock.expectOne(`${BASE_URL}/personal-data`);
    TestBed.inject(SessionEventsService).notifySessionEnded();
    expect(request.cancelled).toBe(true);
    expect(completed).toBe(true);
    expect(createObjectUrlSpy).not.toHaveBeenCalled();
});

let service: ExportService;
let httpMock: HttpTestingController;
let createObjectUrlSpy: ReturnType<typeof vi.spyOn>;
let revokeObjectUrlSpy: ReturnType<typeof vi.spyOn>;
let clickedDownloadName: string | null;

beforeEach(() => {
    configureExportClient();
    createObjectUrlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue(BLOB_URL);
    revokeObjectUrlSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    clickedDownloadName = null;
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
        clickedDownloadName = this.download;
    });
});

function configureExportClient(platform: 'browser' | 'server' = 'browser'): void {
    TestBed.configureTestingModule({
        providers: [
            ExportService,
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: PLATFORM_ID, useValue: platform },
            { provide: BrowserWindowService, useValue: { getOrigin: (): string => REPORT_ORIGIN } },
        ],
    });
    service = TestBed.inject(ExportService);
    httpMock = TestBed.inject(HttpTestingController);
}

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

describe('ExportService authenticated cycle archive', () => {
    it('posts the confirmed range and downloads the response using the sensitive archive fallback name', () => {
        const selection = {
            dateFrom: '2026-05-01T00:00:00.000Z',
            dateTo: '2026-05-14T23:59:59.999Z',
            timeZoneOffsetMinutes: 240,
            currentPassword: 'test-only-confirmation',
        };
        const completed = vi.fn();
        service.exportSensitiveCycle(selection).subscribe({ complete: completed });
        const request = httpMock.expectOne({ method: 'POST', url: `${BASE_URL}/cycle/sensitive` });
        expect(request.request.body).toEqual(selection);
        expect(request.request.headers.get('Accept')).toBe('text/csv');
        expect(request.request.responseType).toBe('blob');
        const archive = new Blob(['cycle data'], { type: 'text/csv' });
        request.flush(archive, { headers: { 'Content-Disposition': 'attachment' } });

        expect(completed).toHaveBeenCalledOnce();
        expect(createObjectUrlSpy).toHaveBeenCalledExactlyOnceWith(archive);
        expect(clickedDownloadName).toBe('cycle-tracking-sensitive.csv');
        expect(revokeObjectUrlSpy).toHaveBeenCalledExactlyOnceWith(BLOB_URL);
    });

    it('reports rejected confirmation without downloading and retries with the corrected password', () => {
        const range = { dateFrom: '2026-05-01', dateTo: '2026-05-14' };
        const error = vi.fn();
        service.exportSensitiveCycle({ ...range, currentPassword: 'test-rejected' }).subscribe({ error });
        httpMock.expectOne(`${BASE_URL}/cycle/sensitive`).flush(new Blob(), {
            status: HttpStatusCode.Unauthorized,
            statusText: 'Unauthorized',
        });
        expect(error).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ status: HttpStatusCode.Unauthorized }));
        expect(createObjectUrlSpy).not.toHaveBeenCalled();

        const corrected = { ...range, currentPassword: 'test-corrected' };
        service.exportSensitiveCycle(corrected).subscribe();
        const retry = httpMock.expectOne({ method: 'POST', url: `${BASE_URL}/cycle/sensitive` });
        expect(retry.request.body).toEqual(corrected);
        retry.flush(new Blob(['cycle data'], { type: 'text/csv' }));

        expect(clickedDownloadName).toBe('cycle-tracking-sensitive.csv');
    });

    it('uses the supplied archive name when the server name cannot be percent decoded', () => {
        service.exportPersonalData().subscribe();
        const request = httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/personal-data` });
        expect(request.request.headers.get('Accept')).toBe('application/zip');
        request.flush(new Blob(['archive'], { type: 'application/zip' }), {
            headers: { 'Content-Disposition': "attachment; filename*=UTF-8''personal%archive.zip" },
        });

        expect(clickedDownloadName).toBe('personal%archive.zip');
        expect(revokeObjectUrlSpy).toHaveBeenCalledExactlyOnceWith(BLOB_URL);
    });
});

describe('ExportService download ownership and platform', () => {
    it('skips a deferred export created by the previous session and downloads one created by the current session', async () => {
        const old = service.exportPersonalData();
        const sessions = TestBed.inject(SessionEventsService);
        sessions.notifySessionEnded();
        sessions.notifyAuthenticated();

        const oldDownload = firstValueFrom(old);
        const url = `${BASE_URL}/personal-data`;
        httpMock.expectOne({ method: 'GET', url }).flush(new Blob(['deferred archive'], { type: 'application/zip' }));
        await oldDownload;
        expect(createObjectUrlSpy).not.toHaveBeenCalled();

        const currentDownload = firstValueFrom(service.exportPersonalData());
        const archive = new Blob(['new archive'], { type: 'application/zip' });
        httpMock.expectOne({ method: 'GET', url }).flush(archive);
        await currentDownload;
        expect(createObjectUrlSpy).toHaveBeenCalledExactlyOnceWith(archive);
        expect(clickedDownloadName).toBe('food-diary-personal-data.zip');
    });

    it('completes an export on the server without creating a browser download', async () => {
        TestBed.resetTestingModule();
        configureExportClient('server');
        const pending = firstValueFrom(service.exportPersonalData());
        httpMock.expectOne({ method: 'GET', url: `${BASE_URL}/personal-data` }).flush(new Blob(['archive'], { type: 'application/zip' }));

        await expect(pending).resolves.toBeUndefined();
        expect(createObjectUrlSpy).not.toHaveBeenCalled();
        expect(revokeObjectUrlSpy).not.toHaveBeenCalled();
        expect(clickedDownloadName).toBeNull();
    });
});

import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpClient, type HttpResponse } from '@angular/common/http';
import { inject, PLATFORM_ID, Service } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { map, merge, type Observable, takeUntil } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SessionEventsService } from '../auth/session-events.service';
import type { ExportCycleRequest, ExportDiaryRequest, ExportSensitiveCycleRequest } from '../models/export.models';
import { BrowserWindowService } from '../platform/browser-window.service';
import { ExportSdk } from './sdk/generated/api/export.service';
import { createSdkConnection } from './sdk/sdk-connection';

@Service()
export class ExportService {
    private readonly sessions = inject(SessionEventsService);
    private sessionVersion = 0;

    private readonly document = inject(DOCUMENT);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    protected readonly baseUrl = environment.apiUrls.export;
    private readonly sdk = createSdkConnection(ExportSdk, this.baseUrl, inject(HttpClient));

    public constructor() {
        merge(this.sessions.authenticated$, this.sessions.sessionEnded$)
            .pipe(takeUntilDestroyed())
            .subscribe(() => this.sessionVersion++);
    }
    public exportPersonalData(): Observable<void> {
        return this.saveBlobResponse(
            this.sdk.client.getExportPersonalData({ version: this.sdk.version }, 'response', false, {
                httpHeaderAccept: 'application/zip',
            }),
            'food-diary-personal-data.zip',
        );
    }

    public exportDiary(request: ExportDiaryRequest): Observable<void> {
        const { dateFrom, dateTo, format = 'csv', locale, timeZoneOffsetMinutes } = request;
        const ext = format === 'pdf' ? 'pdf' : 'csv';
        const reportOrigin = this.browserWindow.getOrigin();
        return this.saveBlobResponse(
            this.sdk.client.getExportDiary(
                { version: this.sdk.version, dateFrom, dateTo, format, locale, timeZoneOffsetMinutes, reportOrigin },
                'response',
                false,
                { httpHeaderAccept: format === 'pdf' ? 'application/pdf' : 'text/csv' },
            ),
            `food-diary.${ext}`,
        );
    }

    public exportCycle(request: ExportCycleRequest): Observable<void> {
        const { dateFrom, dateTo, timeZoneOffsetMinutes } = request;
        return this.saveBlobResponse(
            this.sdk.client.getExportCycle({ version: this.sdk.version, dateFrom, dateTo, timeZoneOffsetMinutes }, 'response', false, {
                httpHeaderAccept: 'text/csv',
            }),
            'cycle-tracking.csv',
        );
    }

    public exportSensitiveCycle(request: ExportSensitiveCycleRequest): Observable<void> {
        return this.saveBlobResponse(
            this.sdk.client.postExportCycleSensitive(
                { version: this.sdk.version, sensitiveCycleExportHttpRequest: request },
                'response',
                false,
                { httpHeaderAccept: 'text/csv' },
            ),
            'cycle-tracking-sensitive.csv',
        );
    }

    private saveBlobResponse(response$: Observable<HttpResponse<Blob>>, fallbackFileName: string): Observable<void> {
        const session = this.sessionVersion;
        return response$.pipe(
            takeUntil(merge(this.sessions.authenticated$, this.sessions.sessionEnded$)),
            map(response => {
                if (session !== this.sessionVersion) {
                    return;
                }
                const blob = response.body;
                if (blob === null) {
                    return;
                }

                const contentDisposition = response.headers.get('Content-Disposition');
                const fileName = this.extractFileName(contentDisposition) ?? fallbackFileName;

                if (!this.isBrowser) {
                    return;
                }

                const url = URL.createObjectURL(blob);
                const anchor = this.document.createElement('a');
                anchor.href = url;
                anchor.download = fileName;
                anchor.click();
                URL.revokeObjectURL(url);
            }),
        );
    }

    private extractFileName(contentDisposition: string | null): string | null {
        if (contentDisposition === null || contentDisposition.length === 0) {
            return null;
        }

        const match = /filename\*?=(?:utf-8''|"?)([^";]+)/i.exec(contentDisposition);
        const fileName = match?.[1]?.replaceAll('"', '') ?? null;
        if (fileName === null) {
            return null;
        }

        try {
            return decodeURIComponent(fileName);
        } catch {
            return fileName;
        }
    }
}

import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiService } from '../../services/api.service';
import type { ExportCycleRequest, ExportDiaryRequest } from '../models/export.models';
import { BrowserWindowService } from '../platform/browser-window.service';

@Service()
export class ExportService extends ApiService {
    private readonly document = inject(DOCUMENT);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    protected readonly baseUrl = environment.apiUrls.export;

    public exportDiary(request: ExportDiaryRequest): Observable<void> {
        const { dateFrom, dateTo, format = 'csv', locale, timeZoneOffsetMinutes } = request;
        const ext = format === 'pdf' ? 'pdf' : 'csv';
        const reportOrigin = this.browserWindow.getOrigin();
        return this.downloadBlob('diary', { dateFrom, dateTo, format, locale, timeZoneOffsetMinutes, reportOrigin }, `food-diary.${ext}`);
    }

    public exportCycle(request: ExportCycleRequest): Observable<void> {
        const { dateFrom, dateTo, timeZoneOffsetMinutes } = request;
        return this.downloadBlob('cycle', { dateFrom, dateTo, timeZoneOffsetMinutes }, 'cycle-tracking.csv');
    }

    private downloadBlob(
        endpoint: string,
        params: Record<string, string | number | boolean | null | undefined>,
        fallbackFileName: string,
    ): Observable<void> {
        return this.getBlob(endpoint, params).pipe(
            map(response => {
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

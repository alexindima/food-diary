import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';

export type ExportFormat = 'csv' | 'pdf';

export type ExportDiaryRequest = {
    dateFrom: string;
    dateTo: string;
    format?: ExportFormat;
    locale?: string;
    timeZoneOffsetMinutes?: number;
};

@Injectable({ providedIn: 'root' })
export class ExportService extends ApiService {
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    protected readonly baseUrl = environment.apiUrls.export;

    public exportDiary(request: ExportDiaryRequest): Observable<void> {
        const { dateFrom, dateTo, format = 'csv', locale, timeZoneOffsetMinutes } = request;
        const ext = format === 'pdf' ? 'pdf' : 'csv';
        const reportOrigin = this.isBrowser ? this.document.location.origin : undefined;
        return this.getBlob('diary', { dateFrom, dateTo, format, locale, timeZoneOffsetMinutes, reportOrigin }).pipe(
            map(response => {
                const blob = response.body;
                if (blob === null) {
                    return;
                }

                const contentDisposition = response.headers.get('Content-Disposition');
                const fileName = this.extractFileName(contentDisposition) ?? `food-diary.${ext}`;

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

        const match = /filename\*?=(?:UTF-8''|"?)([^";]+)/i.exec(contentDisposition);
        const fileName = match?.[1]?.replace(/"/g, '') ?? null;
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

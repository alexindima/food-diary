import { Injectable } from '@angular/core';
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

@Injectable({
    providedIn: 'root',
})
export class ExportService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.export;

    public exportDiary(request: ExportDiaryRequest): Observable<void> {
        const { dateFrom, dateTo, format = 'csv', locale, timeZoneOffsetMinutes } = request;
        const ext = format === 'pdf' ? 'pdf' : 'csv';
        const reportOrigin = typeof window === 'undefined' ? undefined : window.location.origin;
        return this.getBlob('diary', { dateFrom, dateTo, format, locale, timeZoneOffsetMinutes, reportOrigin }).pipe(
            map(response => {
                const blob = response.body;
                if (blob === null) {
                    return;
                }

                const contentDisposition = response.headers.get('Content-Disposition');
                const fileName = this.extractFileName(contentDisposition) ?? `food-diary.${ext}`;

                const url = URL.createObjectURL(blob);
                const anchor = document.createElement('a');
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
        return match?.[1]?.replace(/"/g, '') ?? null;
    }
}

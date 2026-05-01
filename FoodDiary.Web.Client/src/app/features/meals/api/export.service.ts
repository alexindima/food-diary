import { Injectable } from '@angular/core';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';

export type ExportFormat = 'csv' | 'pdf';

@Injectable({
    providedIn: 'root',
})
export class ExportService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.export;

    public exportDiary(dateFrom: string, dateTo: string, format: ExportFormat = 'csv'): void {
        const ext = format === 'pdf' ? 'pdf' : 'csv';
        this.getBlob('diary', { dateFrom, dateTo, format }).subscribe(response => {
            const blob = response.body;
            if (!blob) {
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
        });
    }

    private extractFileName(contentDisposition: string | null): string | null {
        if (!contentDisposition) {
            return null;
        }

        const match = contentDisposition.match(/filename\*?=(?:UTF-8''|"?)([^";]+)/i);
        return match?.[1]?.replace(/"/g, '') ?? null;
    }
}

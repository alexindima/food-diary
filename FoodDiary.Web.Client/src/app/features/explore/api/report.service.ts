import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { ContentReport, CreateReportDto } from '../models/report.data';

@Injectable({
    providedIn: 'root',
})
export class ReportService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.reports;

    public create(dto: CreateReportDto): Observable<ContentReport> {
        return this.post<ContentReport>('', dto).pipe(catchError((error: unknown) => rethrowApiError('Create report error', error)));
    }
}

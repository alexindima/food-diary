import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ReportsSdk } from '../../../shared/api/sdk/generated/api/reports.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { ContentReport, CreateReportDto } from '../models/report.data';
import { contentReportFromSdk, createReportToSdk } from './community-sdk.mapper';

@Service()
export class ReportService {
    protected readonly baseUrl = environment.apiUrls.reports;
    private readonly sdk = createSdkConnection(ReportsSdk, this.baseUrl, inject(HttpClient));

    public create(dto: CreateReportDto): Observable<ContentReport> {
        return this.sdk.client.postReports({ version: this.sdk.version, createContentReportHttpRequest: createReportToSdk(dto) }).pipe(
            map(contentReportFromSdk),
            catchError((error: unknown) => rethrowApiError('Create report error', error)),
        );
    }
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminModerationSdk } from '../../../shared/api/sdk/generated/api/admin-moderation.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import type { AdminId } from '../../../shared/models/semantics/admin-meaning';
import type { AdminContentReport, AdminReportAction } from '../models/admin-moderation.data';
import type { PagedResponse } from '../models/admin-moderation-page.models';
import { adminContentReportFromSdk } from './admin-moderation-sdk.mapper';

@Service()
export class AdminModerationService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/moderation`;
    private readonly sdk = createSdkConnection(AdminModerationSdk, this.baseUrl, this.http);

    public getReports(
        page: number,
        limit: number,
        status?: string | null,
        filters: Record<string, string> = {},
    ): Observable<PagedResponse<AdminContentReport>> {
        let params = new HttpParams().set('page', page).set('limit', limit);
        if (status !== null && status !== undefined && status.trim().length > 0) {
            params = params.set('status', status);
        }

        for (const [key, value] of Object.entries(filters)) {
            if (value.length > 0) {
                params = params.set(key, value);
            }
        }
        return this.sdk.client
            .getAdminModeration({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(response => sdkPage(response, adminContentReportFromSdk)))
            .pipe(
                map(response => ({
                    items: response.data,
                    totalPages: response.totalPages,
                    totalItems: response.totalItems,
                })),
            );
    }

    public reviewReport(reportId: AdminId<'content-report'>, action: AdminReportAction): Observable<void> {
        return this.sdk.client.postAdminModerationByIdReview({
            version: this.sdk.version,
            id: reportId,
            adminReportActionHttpRequest: action,
        });
    }

    public dismissReport(reportId: AdminId<'content-report'>, action: AdminReportAction): Observable<void> {
        return this.sdk.client.postAdminModerationByIdDismiss({
            version: this.sdk.version,
            id: reportId,
            adminReportActionHttpRequest: action,
        });
    }
}

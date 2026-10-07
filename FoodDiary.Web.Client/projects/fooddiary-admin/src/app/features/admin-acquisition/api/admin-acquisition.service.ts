import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminAcquisitionSdk } from '../../../shared/api/sdk/generated/api/admin-acquisition.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';
import { marketingAttributionRangeFromSdk, marketingAttributionSummaryFromSdk } from './admin-acquisition-sdk.mapper';

export const DEFAULT_ACQUISITION_WINDOW_HOURS = 720;

@Service()
export class AdminAcquisitionService {
    private readonly http = inject(HttpClient);
    private readonly summaryUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/acquisition/summary`;
    private readonly sdk = createSdkConnection(AdminAcquisitionSdk, this.summaryUrl, this.http);

    public getRange(params: Record<string, string | number>): Observable<MarketingAttributionRange> {
        return this.sdk.client
            .getAdminAcquisitionRange({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(marketingAttributionRangeFromSdk));
    }

    public getSummary(hours: number = DEFAULT_ACQUISITION_WINDOW_HOURS): Observable<MarketingAttributionSummary> {
        return this.sdk.client
            .getAdminAcquisitionSummary({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, { hours }))
            .pipe(
                map(summary => ({
                    ...summary,
                    attributedVisits: summary.attributedVisits ?? 0,
                    organicVisits: summary.organicVisits ?? summary.visits,
                })),
                map(marketingAttributionSummaryFromSdk),
            );
    }
}

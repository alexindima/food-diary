import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminTelemetrySdk } from '../../../shared/api/sdk/generated/api/admin-telemetry.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { FastingTelemetrySummary } from '../models/admin-telemetry.data';
import { fastingTelemetrySummaryFromSdk } from './admin-dashboard-sdk.mapper';

const DEFAULT_FASTING_SUMMARY_HOURS = 24;

@Service()
export class AdminTelemetryService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/telemetry`;
    private readonly sdk = createSdkConnection(AdminTelemetrySdk, this.baseUrl, this.http);

    public getFastingSummary(hours: number = DEFAULT_FASTING_SUMMARY_HOURS): Observable<FastingTelemetrySummary> {
        return this.sdk.client
            .getAdminTelemetryFasting({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, { hours }))
            .pipe(map(fastingTelemetrySummaryFromSdk));
    }
}

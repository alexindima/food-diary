import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminAiUsageSdk } from '../../../shared/api/sdk/generated/api/admin-ai-usage.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { AdminAiUsageSummary } from '../models/admin-ai-usage.data';
import { adminAiUsageSummaryFromSdk } from './admin-ai-usage-sdk.mapper';

@Service()
export class AdminAiUsageService {
    private readonly http = inject(HttpClient);
    private readonly aiUsageUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/ai-usage/summary`;
    private readonly sdk = createSdkConnection(AdminAiUsageSdk, this.aiUsageUrl, this.http);

    public getSummary(range: { from?: string; to?: string; userId?: string } = {}): Observable<AdminAiUsageSummary> {
        return this.sdk.client
            .getAdminAiUsageSummary({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, { ...range }))
            .pipe(map(adminAiUsageSummaryFromSdk));
    }
}

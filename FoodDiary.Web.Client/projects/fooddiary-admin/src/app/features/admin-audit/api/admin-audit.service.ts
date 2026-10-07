import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminAuditSdk } from '../../../shared/api/sdk/generated/api/admin-audit.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { AdminAuditPageResult } from '../models/admin-audit';
import { adminAuditPageFromSdk } from './admin-audit-sdk.mapper';

@Service()
export class AdminAuditService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/audit`;
    private readonly sdk = createSdkConnection(AdminAuditSdk, this.url, this.http);

    public getPage(params: Record<string, string | number>): Observable<AdminAuditPageResult> {
        return this.sdk.client
            .getAdminAudit({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(adminAuditPageFromSdk));
    }
}

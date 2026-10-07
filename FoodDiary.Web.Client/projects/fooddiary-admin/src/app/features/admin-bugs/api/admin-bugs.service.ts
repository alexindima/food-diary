import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminBugsSdk } from '../../../shared/api/sdk/generated/api/admin-bugs.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import type { AdminBugReportPage } from '../models/admin-bug-report';
import { adminBugReportPageFromSdk } from './admin-bugs-sdk.mapper';

@Service()
export class AdminBugsService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/bugs`;
    private readonly sdk = createSdkConnection(AdminBugsSdk, this.url, this.http);

    public getPage(params: Record<string, string | number>): Observable<AdminBugReportPage> {
        return this.sdk.client
            .getAdminBugs({ version: this.sdk.version }, 'body', false, sdkRequestOptions(undefined, params))
            .pipe(map(adminBugReportPageFromSdk));
    }
}

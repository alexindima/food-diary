import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { AdminDailyAdvicesSdk } from '../../../shared/api/sdk/generated/api/admin-daily-advices.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import type {
    AdminDailyAdvice,
    AdminDailyAdvicesImportRequest,
    AdminDailyAdvicesImportResponse,
    AdminDailyAdviceUpdate,
} from '../models/admin-daily-advice.models';
import { adminDailyAdviceGroupFromSdk, adminDailyAdvicesImportFromSdk } from './admin-daily-advices-sdk.mapper';

@Service()
export class AdminDailyAdvicesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/daily-advices`;
    private readonly sdk = createSdkConnection(AdminDailyAdvicesSdk, this.baseUrl, this.http);

    public getAll(): Observable<AdminDailyAdvice[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getAdminDailyAdvicesGroups({ version: this.sdk.version, page, limit })
                .pipe(map(items => items.map(adminDailyAdviceGroupFromSdk))),
        );
    }

    public importAdvices(request: AdminDailyAdvicesImportRequest): Observable<AdminDailyAdvicesImportResponse> {
        const idempotencyKey = crypto.randomUUID();
        const response =
            request.version === 2
                ? this.sdk.client.postAdminDailyAdvicesGroupsImport({
                      version: this.sdk.version,
                      adminDailyAdvicePairsImportHttpRequest: request,
                      idempotencyKey,
                  })
                : this.sdk.client.postAdminDailyAdvicesImport({
                      version: this.sdk.version,
                      adminDailyAdvicesImportHttpRequest: request,
                      idempotencyKey,
                  });
        return response.pipe(map(adminDailyAdvicesImportFromSdk));
    }

    public update(id: string, request: AdminDailyAdviceUpdate): Observable<AdminDailyAdvice> {
        return this.sdk.client
            .putAdminDailyAdvicesGroupsById({ version: this.sdk.version, id, adminDailyAdviceGroupUpdateHttpRequest: request })
            .pipe(map(adminDailyAdviceGroupFromSdk));
    }

    public delete(id: string): Observable<void> {
        return this.sdk.client.deleteAdminDailyAdvicesGroupsById({ version: this.sdk.version, id });
    }
}

import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { loadPagedCollection } from '../../../shared/api/load-paged-collection';
import { AdminEmailTemplatesSdk } from '../../../shared/api/sdk/generated/api/admin-email-templates.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import type {
    AdminEmailTemplate,
    AdminEmailTemplateTestRequest,
    AdminEmailTemplateUpsertRequest,
} from '../models/admin-email-template.data';
import { adminEmailTemplateFromSdk } from './admin-email-templates-sdk.mapper';

@Service()
export class AdminEmailTemplatesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/email-templates`;
    private readonly sdk = createSdkConnection(AdminEmailTemplatesSdk, this.baseUrl, this.http);

    public getAll(): Observable<AdminEmailTemplate[]> {
        return loadPagedCollection((page, limit) =>
            this.sdk.client
                .getAdminEmailTemplates({ version: this.sdk.version, page, limit })
                .pipe(map(items => items.map(adminEmailTemplateFromSdk))),
        );
    }

    public upsert(key: string, locale: string, request: AdminEmailTemplateUpsertRequest): Observable<AdminEmailTemplate> {
        return this.sdk.client
            .putAdminEmailTemplatesByKeyByLocale({ version: this.sdk.version, key, locale, adminEmailTemplateUpsertHttpRequest: request })
            .pipe(map(adminEmailTemplateFromSdk));
    }

    public sendTest(request: AdminEmailTemplateTestRequest): Observable<void> {
        return this.sdk.client.postAdminEmailTemplatesTest({ version: this.sdk.version, adminEmailTemplateTestHttpRequest: request });
    }
}

import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';
import { map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AdminAiPromptsSdk } from '../../../shared/api/sdk/generated/api/admin-ai-prompts.service';
import { AdminEmailTemplatesSdk } from '../../../shared/api/sdk/generated/api/admin-email-templates.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import type { AdminTemplateRevision } from '../models/admin-template-revision';
import { adminTemplateRevisionFromSdk } from './admin-template-history-sdk.mapper';

@Service()
export class AdminTemplateHistoryService {
    private readonly http = inject(HttpClient);
    private readonly url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin`;
    private readonly sdk = createSdkConnection(AdminAiPromptsSdk, this.url, this.http);
    private readonly emailTemplates = createSdkConnection(AdminEmailTemplatesSdk, this.url, this.http);
    public getRevisions(kind: 'email-templates' | 'ai-prompts', key: string, locale: string): Observable<AdminTemplateRevision[]> {
        const response =
            kind === 'ai-prompts'
                ? this.sdk.client.getAdminAiPromptsByKeyByLocaleRevisions({ version: this.sdk.version, key, locale })
                : this.emailTemplates.client.getAdminEmailTemplatesByKeyByLocaleRevisions({
                      version: this.emailTemplates.version,
                      key,
                      locale,
                  });
        return response.pipe(map(items => items.map(adminTemplateRevisionFromSdk)));
    }
}

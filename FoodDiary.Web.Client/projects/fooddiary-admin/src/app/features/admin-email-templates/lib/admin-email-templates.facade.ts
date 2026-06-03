import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminEmailTemplatesService } from '../api/admin-email-templates.service';
import type {
    AdminEmailTemplate,
    AdminEmailTemplateTestRequest,
    AdminEmailTemplateUpsertRequest,
} from '../models/admin-email-template.data';

@Service()
export class AdminEmailTemplatesFacade {
    private readonly templatesService = inject(AdminEmailTemplatesService);

    public getAll(): Observable<AdminEmailTemplate[]> {
        return this.templatesService.getAll();
    }

    public upsert(key: string, locale: string, request: AdminEmailTemplateUpsertRequest): Observable<AdminEmailTemplate> {
        return this.templatesService.upsert(key, locale, request);
    }

    public sendTest(request: AdminEmailTemplateTestRequest): Observable<void> {
        return this.templatesService.sendTest(request);
    }
}

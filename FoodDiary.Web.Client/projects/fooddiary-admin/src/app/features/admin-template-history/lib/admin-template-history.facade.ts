import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminTemplateHistoryService } from '../api/admin-template-history.service';
import type { AdminTemplateRevision } from '../models/admin-template-revision';

@Service()
export class AdminTemplateHistoryFacade {
    private readonly api = inject(AdminTemplateHistoryService);
    public getRevisions(kind: 'email-templates' | 'ai-prompts', key: string, locale: string): Observable<AdminTemplateRevision[]> {
        return this.api.getRevisions(kind, key, locale);
    }
}

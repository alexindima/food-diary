import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAuditService } from '../api/admin-audit.service';
import type { AdminAuditPageResult } from '../models/admin-audit';
@Service()
export class AdminAuditFacade {
    private readonly api = inject(AdminAuditService);
    public getPage(params: Record<string, string | number>): Observable<AdminAuditPageResult> {
        return this.api.getPage(params);
    }
}

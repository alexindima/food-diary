import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminRetentionService } from '../api/admin-retention.service';
import type { AdminRetentionReport } from '../models/admin-retention';

@Service()
export class AdminRetentionFacade {
    private readonly api = inject(AdminRetentionService);
    public getReport(params: { from?: string; to?: string }): Observable<AdminRetentionReport> {
        return this.api.getReport(params);
    }
}

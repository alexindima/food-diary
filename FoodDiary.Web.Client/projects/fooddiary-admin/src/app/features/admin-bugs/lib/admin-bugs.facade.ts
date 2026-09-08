import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminBugsService } from '../api/admin-bugs.service';
import type { AdminBugReportPage } from '../models/admin-bug-report';
@Service()
export class AdminBugsFacade {
    private readonly api = inject(AdminBugsService);
    public getPage(params: Record<string, string | number>): Observable<AdminBugReportPage> {
        return this.api.getPage(params);
    }
}

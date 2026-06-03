import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminModerationService } from '../api/admin-moderation.service';
import type { AdminContentReport, AdminReportAction } from '../models/admin-moderation.data';
import type { PagedResponse } from '../models/admin-moderation-page.models';

@Service()
export class AdminModerationFacade {
    private readonly moderationService = inject(AdminModerationService);

    public getReports(page: number, limit: number, status?: string | null): Observable<PagedResponse<AdminContentReport>> {
        return this.moderationService.getReports(page, limit, status);
    }

    public reviewReport(reportId: string, action: AdminReportAction): Observable<void> {
        return this.moderationService.reviewReport(reportId, action);
    }

    public dismissReport(reportId: string, action: AdminReportAction): Observable<void> {
        return this.moderationService.dismissReport(reportId, action);
    }
}

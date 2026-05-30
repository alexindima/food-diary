import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAiUsageService } from '../../admin-ai-usage/api/admin-ai-usage.service';
import type { AdminAiUsageSummary } from '../../admin-ai-usage/models/admin-ai-usage.data';
import { AdminUsersFacade } from '../../admin-users/lib/admin-users.facade';
import type { AdminUserLoginDeviceSummary } from '../../admin-users/models/admin-user.models';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminTelemetryService } from '../api/admin-telemetry.service';
import type { AdminDashboardSummary } from '../models/admin-dashboard.data';
import type { FastingTelemetrySummary } from '../models/admin-telemetry.data';

@Injectable({ providedIn: 'root' })
export class AdminDashboardFacade {
    private readonly dashboardService = inject(AdminDashboardService);
    private readonly aiUsageService = inject(AdminAiUsageService);
    private readonly telemetryService = inject(AdminTelemetryService);
    private readonly usersFacade = inject(AdminUsersFacade);

    public getSummary(): Observable<AdminDashboardSummary> {
        return this.dashboardService.getSummary();
    }

    public getAiUsageSummary(): Observable<AdminAiUsageSummary> {
        return this.aiUsageService.getSummary();
    }

    public getFastingSummary(): Observable<FastingTelemetrySummary> {
        return this.telemetryService.getFastingSummary();
    }

    public getLoginSummary(): Observable<AdminUserLoginDeviceSummary[]> {
        return this.usersFacade.getLoginSummary();
    }
}

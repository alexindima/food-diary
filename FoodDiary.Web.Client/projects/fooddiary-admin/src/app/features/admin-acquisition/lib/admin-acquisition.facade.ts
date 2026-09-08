import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAcquisitionService, DEFAULT_ACQUISITION_WINDOW_HOURS } from '../api/admin-acquisition.service';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';
import type { MarketingAttributionRange } from '../models/admin-acquisition-range';

export const DEFAULT_ADMIN_ACQUISITION_WINDOW_HOURS = DEFAULT_ACQUISITION_WINDOW_HOURS;

@Service()
export class AdminAcquisitionFacade {
    private readonly acquisitionService = inject(AdminAcquisitionService);

    public getRange(params: Record<string, string | number>): Observable<MarketingAttributionRange> {
        return this.acquisitionService.getRange(params);
    }

    public getSummary(hours?: number): Observable<MarketingAttributionSummary> {
        return this.acquisitionService.getSummary(hours);
    }
}

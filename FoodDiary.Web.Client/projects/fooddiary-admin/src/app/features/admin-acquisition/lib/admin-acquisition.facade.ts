import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAcquisitionService, DEFAULT_ACQUISITION_WINDOW_HOURS } from '../api/admin-acquisition.service';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';

export const DEFAULT_ADMIN_ACQUISITION_WINDOW_HOURS = DEFAULT_ACQUISITION_WINDOW_HOURS;

@Service()
export class AdminAcquisitionFacade {
    private readonly acquisitionService = inject(AdminAcquisitionService);

    public getSummary(hours?: number): Observable<MarketingAttributionSummary> {
        return this.acquisitionService.getSummary(hours);
    }
}

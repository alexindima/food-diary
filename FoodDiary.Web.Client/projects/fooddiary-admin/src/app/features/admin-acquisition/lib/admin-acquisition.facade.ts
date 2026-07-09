import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAcquisitionService } from '../api/admin-acquisition.service';
import type { MarketingAttributionSummary } from '../models/admin-acquisition.data';

@Service()
export class AdminAcquisitionFacade {
    private readonly acquisitionService = inject(AdminAcquisitionService);

    public getSummary(): Observable<MarketingAttributionSummary> {
        return this.acquisitionService.getSummary();
    }
}

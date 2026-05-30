import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import { AdminAiUsageService } from '../api/admin-ai-usage.service';
import type { AdminAiUsageSummary } from '../models/admin-ai-usage.data';

@Injectable({ providedIn: 'root' })
export class AdminAiUsageFacade {
    private readonly aiUsageService = inject(AdminAiUsageService);

    public getSummary(): Observable<AdminAiUsageSummary> {
        return this.aiUsageService.getSummary();
    }
}

import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { HydrationEntry } from '../../../shared/models/hydration.data';

export type HydrationActions = {
    addEntry: (amountMl: number, timestampUtc: Date) => Observable<HydrationEntry>;
};

export const HYDRATION_ACTIONS = new InjectionToken<HydrationActions>('HydrationActions');

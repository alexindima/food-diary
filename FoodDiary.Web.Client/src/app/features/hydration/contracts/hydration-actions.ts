import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { HydrationEntry } from '../../../shared/models/hydration.data';
import type { HydrationEntryId } from '../../../shared/models/semantics/entity-id';

export type HydrationActions = {
    addEntry: (amountMl: number, timestampUtc: Date) => Observable<HydrationEntry>;
    deleteEntry: (id: HydrationEntryId) => Observable<void>;
};

export const HYDRATION_ACTIONS = new InjectionToken<HydrationActions>('HydrationActions');

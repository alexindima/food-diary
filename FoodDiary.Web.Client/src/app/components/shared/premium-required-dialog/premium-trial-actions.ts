import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

export type PremiumTrialActions = {
    canStart: () => Observable<boolean>;
    start: () => Observable<unknown>;
};

export const PREMIUM_TRIAL_ACTIONS = new InjectionToken<PremiumTrialActions>('Premium trial actions');

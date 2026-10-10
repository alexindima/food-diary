import { DestroyRef, inject, Service } from '@angular/core';

import { GLOBAL_LOADING_TIMING } from '../config/runtime-ui.tokens';
import { LoadingVisibility } from '../shared/ui/loading/loading-visibility';

@Service()
export class GlobalLoadingService {
    private readonly visibility = new LoadingVisibility(inject(GLOBAL_LOADING_TIMING), inject(DestroyRef));
    public readonly isVisible = this.visibility.isVisible;

    public trackRequest(): () => void {
        this.visibility.begin();
        let completed = false;
        return () => {
            if (!completed) {
                completed = true;
                this.visibility.end();
            }
        };
    }
}

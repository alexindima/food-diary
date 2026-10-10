import { DestroyRef, inject, Service } from '@angular/core';

import { ROUTE_LOADING_TIMING } from '../config/runtime-ui.tokens';
import { LoadingVisibility } from '../shared/ui/loading/loading-visibility';

@Service()
export class RouteLoadingService {
    private readonly visibility = new LoadingVisibility(inject(ROUTE_LOADING_TIMING), inject(DestroyRef));
    public readonly isVisible = this.visibility.isVisible;

    public beginLoad(): void {
        this.visibility.begin();
    }

    public endLoad(): void {
        this.visibility.end();
    }
}

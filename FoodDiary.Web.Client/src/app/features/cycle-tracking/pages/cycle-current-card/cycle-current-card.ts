import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import { CyclePredictionBlockComponent } from '../cycle-prediction-block/cycle-prediction-block';
import type { CyclePredictionViewModel, CycleViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-current-card',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent, FdUiCardComponent, CyclePredictionBlockComponent],
    templateUrl: './cycle-current-card.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleCurrentCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly current = input<CycleViewModel | null>(null);
    public readonly prediction = input<CyclePredictionViewModel | null>(null);

    protected readonly titleKey = computed(() => (this.current() !== null ? 'CYCLE_TRACKING.CURRENT_CYCLE' : 'CYCLE_TRACKING.NO_CYCLE'));
}

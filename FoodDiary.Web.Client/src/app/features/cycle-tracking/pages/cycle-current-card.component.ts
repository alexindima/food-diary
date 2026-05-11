import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { CyclePredictionViewModel, CycleViewModel } from './cycle-tracking-page.models';

@Component({
    selector: 'fd-cycle-current-card',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent, FdUiCardComponent],
    templateUrl: './cycle-current-card.component.html',
    styleUrl: './cycle-tracking-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleCurrentCardComponent {
    public readonly titleKey = input.required<string>();
    public readonly isLoading = input.required<boolean>();
    public readonly current = input<CycleViewModel | null>(null);
    public readonly prediction = input<CyclePredictionViewModel | null>(null);
}

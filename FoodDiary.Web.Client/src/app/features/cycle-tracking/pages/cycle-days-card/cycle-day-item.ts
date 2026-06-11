import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface';

import type { CycleDayViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-day-item',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent],
    templateUrl: './cycle-day-item.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDayItemComponent {
    public readonly item = input.required<CycleDayViewModel>();
}

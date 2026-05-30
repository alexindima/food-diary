import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { CycleDayViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-days-card',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent, FdUiCardComponent],
    templateUrl: './cycle-days-card.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDaysCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<CycleDayViewModel[]>();
}

import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { CycleDayViewModel } from './cycle-tracking-page.models';

@Component({
    selector: 'fd-cycle-days-card',
    imports: [TranslatePipe, FdUiAccentSurfaceComponent, FdUiCardComponent],
    templateUrl: './cycle-days-card.component.html',
    styleUrl: './cycle-tracking-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDaysCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<CycleDayViewModel[]>();
}

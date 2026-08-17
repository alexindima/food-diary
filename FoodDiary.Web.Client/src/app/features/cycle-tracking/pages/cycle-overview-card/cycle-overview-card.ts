import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { CycleOverviewViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';

@Component({
    selector: 'fd-cycle-overview-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent],
    templateUrl: './cycle-overview-card.html',
    styleUrl: './cycle-overview-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleOverviewCardComponent {
    public readonly overview = input.required<CycleOverviewViewModel>();
    public readonly dateSelected = output<string>();
    public readonly logToday = output();
}

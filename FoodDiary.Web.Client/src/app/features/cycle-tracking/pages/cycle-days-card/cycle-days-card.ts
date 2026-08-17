import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { CycleDayViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';
import { CycleDayItemComponent } from './cycle-day-item';

@Component({
    selector: 'fd-cycle-days-card',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCardComponent, CycleDayItemComponent],
    templateUrl: './cycle-days-card.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDaysCardComponent {
    public readonly titleKey = input('CYCLE_TRACKING.DAYS_TITLE');
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<CycleDayViewModel[]>();
    public readonly clearingDate = input<string | null>(null);
    public readonly hasMore = input(false);
    public readonly isExpanded = input(false);
    public readonly editDay = output<string>();
    public readonly clearDay = output<string>();
    public readonly confirmPeriodStart = output<string>();
    public readonly historyToggled = output();
}

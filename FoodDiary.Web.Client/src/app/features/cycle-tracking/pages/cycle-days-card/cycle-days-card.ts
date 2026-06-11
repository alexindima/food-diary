import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';

import type { CycleDayViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';
import { CycleDayItemComponent } from './cycle-day-item';

@Component({
    selector: 'fd-cycle-days-card',
    imports: [TranslatePipe, FdUiCardComponent, CycleDayItemComponent],
    templateUrl: './cycle-days-card.html',
    styleUrl: '../cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDaysCardComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly items = input.required<CycleDayViewModel[]>();
    public readonly clearingDate = input<string | null>(null);
    public readonly clearDay = output<string>();
}

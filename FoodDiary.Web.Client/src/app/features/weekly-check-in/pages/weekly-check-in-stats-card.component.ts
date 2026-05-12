import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import type { WeekSummary } from '../models/weekly-check-in.data';

@Component({
    selector: 'fd-weekly-check-in-stats-card',
    standalone: true,
    imports: [DecimalPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './weekly-check-in-stats-card.component.html',
    styleUrl: './weekly-check-in-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyCheckInStatsCardComponent {
    public readonly week = input<WeekSummary | undefined>();
}

import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';

import type { WeeklyCheckInTrendCardViewModel } from '../../lib/weekly-check-in.types';

@Component({
    selector: 'fd-weekly-check-in-trends',
    imports: [DecimalPipe, TranslatePipe, FdUiIconComponent, FdUiAccentSurfaceComponent],
    templateUrl: './weekly-check-in-trends.component.html',
    styleUrl: '../../pages/weekly-check-in-page/weekly-check-in-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyCheckInTrendsComponent {
    public readonly trends = input.required<WeeklyCheckInTrendCardViewModel[]>();
}

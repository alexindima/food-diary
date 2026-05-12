import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { FastingCheckInViewModel } from '../../pages/fasting-page.types';

@Component({
    selector: 'fd-fasting-check-in-summary',
    imports: [TranslatePipe],
    templateUrl: './fasting-check-in-summary.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingCheckInSummaryComponent {
    public readonly latestCheckIn = input.required<FastingCheckInViewModel | null>();
}

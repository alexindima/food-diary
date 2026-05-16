import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { FastingCheckInViewModel } from '../../pages/fasting-page-lib/fasting-page.types';

@Component({
    selector: 'fd-fasting-history-check-in-entry',
    imports: [],
    templateUrl: './fasting-history-check-in-entry.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingHistoryCheckInEntryComponent {
    public readonly checkIn = input.required<FastingCheckInViewModel>();
}

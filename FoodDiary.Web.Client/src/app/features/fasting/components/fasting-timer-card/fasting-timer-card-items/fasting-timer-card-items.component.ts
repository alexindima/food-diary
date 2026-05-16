import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { FastingTimerCardDisplayItem } from '../fasting-timer-card-lib/fasting-timer-card.types';

@Component({
    selector: 'fd-fasting-timer-card-items',
    imports: [],
    templateUrl: './fasting-timer-card-items.component.html',
    styleUrl: './fasting-timer-card-items.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardItemsComponent {
    public readonly items = input.required<readonly FastingTimerCardDisplayItem[]>();
}

import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { FastingTimerCardDisplayItem } from './fasting-timer-card-items.component';

export type FastingTimerCardDisplayGroup = {
    className: string;
    items: FastingTimerCardDisplayItem[];
};

@Component({
    selector: 'fd-fasting-timer-card-groups',
    imports: [],
    templateUrl: './fasting-timer-card-groups.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardGroupsComponent {
    public readonly groups = input.required<readonly FastingTimerCardDisplayGroup[]>();
}

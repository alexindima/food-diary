import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import type { FastingTimerCardDisplayGroup } from '../fasting-timer-card-lib/fasting-timer-card.types';

@Component({
    selector: 'fd-fasting-timer-card-groups',
    imports: [],
    templateUrl: './fasting-timer-card-groups.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardGroupsComponent {
    public readonly groups = input.required<readonly FastingTimerCardDisplayGroup[]>();
}

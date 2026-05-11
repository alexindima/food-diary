import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface FastingTimerCardDisplayItem {
    className: string;
    text: string;
}

@Component({
    selector: 'fd-fasting-timer-card-items',
    imports: [],
    templateUrl: './fasting-timer-card-items.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardItemsComponent {
    public readonly items = input.required<readonly FastingTimerCardDisplayItem[]>();
}

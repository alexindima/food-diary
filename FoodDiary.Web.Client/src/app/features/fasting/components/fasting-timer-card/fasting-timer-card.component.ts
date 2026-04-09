import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-fasting-timer-card',
    standalone: true,
    imports: [TranslatePipe, FdUiCardComponent],
    templateUrl: './fasting-timer-card.component.html',
    styleUrl: './fasting-timer-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FastingTimerCardComponent {
    protected readonly Math = Math;
    public readonly isActive = input<boolean>(false);
    public readonly isOvertime = input<boolean>(false);
    public readonly currentSessionCompleted = input<boolean>(false);
    public readonly progressPercent = input<number>(0);
    public readonly elapsedFormatted = input<string>('00:00:00');
    public readonly remainingFormatted = input<string>('00:00:00');
    public readonly labelKey = input<string>('FASTING.WIDGET_LABEL');
    public readonly stateLabel = input<string | null>(null);
    public readonly detailLabel = input<string | null>(null);
}

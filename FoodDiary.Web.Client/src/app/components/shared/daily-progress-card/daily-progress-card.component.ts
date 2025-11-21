import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { DynamicProgressBarComponent } from '../dynamic-progress-bar/dynamic-progress-bar.component';
import { LocalizedDatePipe } from '../../../pipes/localized-date.pipe';

@Component({
    selector: 'fd-daily-progress-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, LocalizedDatePipe, FdUiCardComponent, DynamicProgressBarComponent],
    templateUrl: './daily-progress-card.component.html',
    styleUrl: './daily-progress-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [DatePipe],
})
export class DailyProgressCardComponent {
    public readonly date = input.required<Date>();
    public readonly consumed = input<number>(0);
    public readonly goal = input<number>(0);

    public readonly progressPercent = computed(() => {
        const goal = this.goal();
        if (!goal || goal <= 0) {
            return 0;
        }
        return Math.round(Math.max(0, (this.consumed() / goal) * 100));
    });

    public readonly remaining = computed(() => {
        const remaining = this.goal() - this.consumed();
        return remaining > 0 ? remaining : 0;
    });
}

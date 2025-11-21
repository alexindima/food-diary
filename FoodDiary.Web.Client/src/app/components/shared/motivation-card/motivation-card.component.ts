import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

@Component({
    selector: 'fd-motivation-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiCardComponent],
    templateUrl: './motivation-card.component.html',
    styleUrl: './motivation-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MotivationCardComponent {
    public readonly consumed = input<number>(0);
    public readonly goal = input<number>(0);

    public readonly percent = computed(() => {
        const goal = this.goal();
        if (!goal || goal <= 0) {
            return 0;
        }
        const pct = (this.consumed() / goal) * 100;
        return Math.round(Math.max(0, pct));
    });

    public readonly messageKey = computed(() => {
        const value = this.percent();
        if (value < 25) {
            return 'MOTIVATION_CARD.EARLY';
        }
        if (value < 60) {
            return 'MOTIVATION_CARD.MID';
        }
        if (value < 100) {
            return 'MOTIVATION_CARD.NEARLY';
        }
        return 'MOTIVATION_CARD.ABOVE';
    });
}

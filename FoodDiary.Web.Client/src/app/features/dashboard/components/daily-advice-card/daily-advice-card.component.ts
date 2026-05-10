import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';
import { type DailyAdvice } from '../../models/daily-advice.data';

@Component({
    selector: 'fd-daily-advice-card',
    standalone: true,
    imports: [CommonModule, FdUiIconComponent, TranslatePipe, DashboardWidgetFrameComponent],
    templateUrl: './daily-advice-card.component.html',
    styleUrl: './daily-advice-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DailyAdviceCardComponent {
    public readonly advice = input<DailyAdvice | null>(null);
    public readonly isLoading = input<boolean>(false);

    public readonly adviceState = computed(() => {
        const advice = this.advice();
        if (!advice) {
            return null;
        }

        return {
            value: advice.value,
            tagKey: advice.tag ? `DASHBOARD.ADVICE_TAGS.${advice.tag.toUpperCase()}` : null,
        };
    });
}

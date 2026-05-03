import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import { type DailyAdvice } from '../../models/daily-advice.data';
import { DashboardWidgetFrameComponent } from '../dashboard-widget-frame/dashboard-widget-frame.component';

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

    public tagLabel(): string | null {
        const tag = this.advice()?.tag;
        if (!tag) {
            return null;
        }

        return tag.replace(/_/g, ' ').replace(/\b\w/g, letter => letter.toUpperCase());
    }
}

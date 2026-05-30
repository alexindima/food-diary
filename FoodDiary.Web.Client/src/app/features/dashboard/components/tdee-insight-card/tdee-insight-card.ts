import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { getEffectiveTdee } from '../../lib/tdee-insight-view.mapper';
import type { TdeeInsight } from '../../models/tdee-insight.data';
import { TdeeInsightCardContentComponent } from './tdee-insight-card-content/tdee-insight-card-content';

@Component({
    selector: 'fd-tdee-insight-card',
    imports: [FdUiIconComponent, TranslatePipe, DashboardWidgetFrameComponent, TdeeInsightCardContentComponent],
    templateUrl: './tdee-insight-card.html',
    styleUrl: './tdee-insight-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TdeeInsightCardComponent {
    public readonly insight = input.required<TdeeInsight | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly applyGoal = output<number>();

    protected readonly effectiveTdee = computed(() => getEffectiveTdee(this.insight()));

    protected onApplyGoal(event?: Event): void {
        event?.stopPropagation();

        const target = this.insight()?.suggestedCalorieTarget;
        if (target !== null && target !== undefined) {
            this.applyGoal.emit(target);
        }
    }
}

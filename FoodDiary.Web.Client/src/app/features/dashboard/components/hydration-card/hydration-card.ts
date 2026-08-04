import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { NoticeBannerComponent } from '../../../../components/shared/notice-banner/notice-banner';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';
import { HYDRATION_CARD_ADD_AMOUNTS_ML, HYDRATION_CARD_MAX_PERCENT, HYDRATION_CARD_PRIMARY_ADD_AMOUNT_ML } from './hydration-card.config';

@Component({
    selector: 'fd-hydration-card',
    imports: [CommonModule, FdUiButtonComponent, TranslatePipe, NoticeBannerComponent, DashboardWidgetFrameComponent],
    templateUrl: './hydration-card.html',
    styleUrl: './hydration-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HydrationCardComponent {
    public readonly total = input.required<number>();
    public readonly goal = input.required<number | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly canAdd = input.required<boolean>();
    public readonly addClick = output<number>();
    public readonly goalAction = output();

    protected readonly addAmounts = HYDRATION_CARD_ADD_AMOUNTS_ML;
    protected readonly primaryAddAmount = HYDRATION_CARD_PRIMARY_ADD_AMOUNT_ML;
    protected readonly hasGoal = computed(() => {
        const goal = this.goal();
        return goal !== null && goal > 0;
    });
    protected readonly percent = computed(() => {
        if (!this.hasGoal()) {
            return 0;
        }
        const value = (this.total() / (this.goal() ?? 1)) * PERCENT_MULTIPLIER;
        return Math.max(0, Math.min(value, HYDRATION_CARD_MAX_PERCENT)); // allow slight overflow visualization
    });
    protected readonly fillLevel = computed(() => `${Math.min(this.percent(), PERCENT_MULTIPLIER)}%`);
    protected readonly progressValue = computed(() => Math.min(Math.max(0, this.total()), this.goal() ?? 0));
    protected readonly remaining = computed(() => Math.max(0, (this.goal() ?? 0) - this.total()));
    protected readonly isGoalReached = computed(() => this.hasGoal() && this.remaining() === 0);

    protected onAdd(amount: number): void {
        if (!this.canAdd()) {
            return;
        }
        if (!(HYDRATION_CARD_ADD_AMOUNTS_ML as readonly number[]).includes(amount)) {
            return;
        }
        this.addClick.emit(amount);
    }

    protected onGoalAction(): void {
        this.goalAction.emit();
    }
}

import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame.component';
import { NoticeBannerComponent } from '../../../../components/shared/notice-banner/notice-banner.component';
import { PERCENT_MULTIPLIER } from '../../../../shared/lib/nutrition.constants';

const ADD_STEP_ML = 250;
const MAX_PERCENT = 200;
const TRACK_WIDTH_MAX_PERCENT = 130;

@Component({
    selector: 'fd-hydration-card',
    standalone: true,
    imports: [CommonModule, FdUiButtonComponent, TranslatePipe, NoticeBannerComponent, DashboardWidgetFrameComponent],
    templateUrl: './hydration-card.component.html',
    styleUrl: './hydration-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HydrationCardComponent {
    private readonly addStep = ADD_STEP_ML;

    public readonly total = input.required<number>();
    public readonly goal = input.required<number | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly canAdd = input.required<boolean>();
    public readonly addClick = output<number>();
    public readonly goalAction = output();

    public readonly addAmount = computed(() => Math.max(1, this.addStep));
    public readonly hasGoal = computed(() => {
        const goal = this.goal();
        return goal !== null && goal > 0;
    });
    public readonly percent = computed(() => {
        if (!this.hasGoal()) {
            return 0;
        }
        const value = (this.total() / (this.goal() ?? 1)) * PERCENT_MULTIPLIER;
        return Math.max(0, Math.min(value, MAX_PERCENT)); // allow slight overflow visualization
    });

    public readonly trackWidth = computed(() => `${Math.min(this.percent(), TRACK_WIDTH_MAX_PERCENT)}%`);

    public onAdd(): void {
        if (!this.canAdd()) {
            return;
        }
        this.addClick.emit(this.addAmount());
    }

    public onGoalAction(): void {
        this.goalAction.emit();
    }
}

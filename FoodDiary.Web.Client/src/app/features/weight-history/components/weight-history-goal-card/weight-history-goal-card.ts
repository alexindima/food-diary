import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { getWeightRemainingToGoal } from '../../lib/weight-history-progress.utils';
import type { WeightEntry } from '../../models/weight-entry.data';

const PERCENT_MAX = 100;
const DAYS_PER_WEEK = 7;
const MILLISECONDS_PER_DAY = 86_400_000;

@Component({
    selector: 'fd-weight-history-goal-card',
    imports: [DecimalPipe, FormField, FdUiButtonComponent, FdUiCardComponent, FdUiIconComponent, FdUiInputComponent, TranslatePipe],
    templateUrl: './weight-history-goal-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryGoalCardComponent {
    public readonly weightField = input.required<FieldTree<string>>();
    public readonly isSaving = input.required<boolean>();
    public readonly entries = input.required<readonly WeightEntry[]>();
    public readonly currentWeight = input.required<number | null>();
    public readonly desiredWeight = input.required<number | null>();

    public readonly saveGoal = output();

    protected readonly isEditing = signal(false);
    protected readonly progress = computed(() => {
        const current = this.currentWeight();
        const goal = this.desiredWeight();
        const oldest = this.entries().at(-1)?.weight;
        if (current === null || goal === null || oldest === undefined) {
            return null;
        }

        const goalDirection = Math.sign(goal - oldest);
        const totalDistance = Math.abs(goal - oldest);
        const completedDistance = (current - oldest) * goalDirection;
        const percent =
            totalDistance === 0 ? PERCENT_MAX : Math.min(PERCENT_MAX, Math.max(0, (completedDistance / totalDistance) * PERCENT_MAX));
        const lost = completedDistance;
        const remaining = getWeightRemainingToGoal(oldest, current, goal);
        const daysElapsed = this.daysBetweenEntries();
        const weeklyRate = daysElapsed > 0 ? (completedDistance / daysElapsed) * DAYS_PER_WEEK : 0;
        const daysToGoal = weeklyRate > 0 ? Math.ceil((remaining / weeklyRate) * DAYS_PER_WEEK) : null;

        return { percent, lost, remaining, weeklyRate, daysToGoal, startWeight: oldest, currentWeight: current, goalWeight: goal };
    });

    protected startEditing(): void {
        this.isEditing.set(true);
    }

    protected cancelEditing(): void {
        this.isEditing.set(false);
    }

    protected save(): void {
        this.saveGoal.emit();
        this.isEditing.set(false);
    }

    private daysBetweenEntries(): number {
        const entries = this.entries();
        const latestDate = entries.at(0)?.date;
        const oldestDate = entries.at(-1)?.date;
        if (latestDate === undefined || oldestDate === undefined) {
            return 0;
        }

        const difference = new Date(latestDate).getTime() - new Date(oldestDate).getTime();
        return Number.isFinite(difference) ? Math.max(0, difference / MILLISECONDS_PER_DAY) : 0;
    }
}

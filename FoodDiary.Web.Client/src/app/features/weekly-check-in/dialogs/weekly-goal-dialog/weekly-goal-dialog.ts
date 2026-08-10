import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';
import { FdUiSwitchComponent } from 'fd-ui-kit/switch/fd-ui-switch';
import { FdUiTimeInputComponent } from 'fd-ui-kit/time-input/fd-ui-time-input';

import type { UpsertWeeklyGoalPayload, WeeklyGoal } from '../../models/weekly-goal.data';

const DEFAULT_TARGET_DAYS = '5';
const DEFAULT_REMINDER_TIME = '21:00';

export type WeeklyGoalDialogData = {
    weekStart: string;
    titleKey: string;
    goal: WeeklyGoal | null;
    saveGoalAsync: (payload: UpsertWeeklyGoalPayload) => Promise<WeeklyGoal | null>;
};

@Component({
    selector: 'fd-weekly-goal-dialog',
    imports: [
        TranslatePipe,
        FdUiButtonComponent,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiSegmentedToggleComponent,
        FdUiSwitchComponent,
        FdUiTimeInputComponent,
    ],
    templateUrl: './weekly-goal-dialog.html',
    styleUrl: './weekly-goal-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeeklyGoalDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<WeeklyGoalDialogComponent, WeeklyGoal | null>);
    protected readonly data = inject<WeeklyGoalDialogData>(FD_UI_DIALOG_DATA);

    protected readonly targetOptions: FdUiSegmentedToggleOption[] = [
        { value: '3', labelKey: 'WEEKLY_CHECK_IN.GOAL.TARGET_3' },
        { value: '5', labelKey: 'WEEKLY_CHECK_IN.GOAL.TARGET_5' },
        { value: '7', labelKey: 'WEEKLY_CHECK_IN.GOAL.TARGET_7' },
    ];
    protected readonly selectedTarget = signal(String(this.data.goal?.targetDays ?? DEFAULT_TARGET_DAYS));
    protected readonly reminderEnabled = signal(this.data.goal?.reminderEnabled ?? false);
    protected readonly reminderTime = signal(this.normalizeTime(this.data.goal?.reminderTime));
    protected readonly isSaving = signal(false);
    protected readonly saveFailed = signal(false);

    protected toggleReminder(enabled: boolean): void {
        this.reminderEnabled.set(enabled);
    }

    protected save(): void {
        void this.saveAsync();
    }

    private async saveAsync(): Promise<void> {
        if (this.isSaving()) {
            return;
        }

        this.isSaving.set(true);
        this.saveFailed.set(false);
        const reminderEnabled = this.reminderEnabled();
        const payload: UpsertWeeklyGoalPayload = {
            weekStart: this.data.weekStart,
            targetDays: Number(this.selectedTarget()),
            reminderEnabled,
            reminderTime: reminderEnabled ? this.reminderTime() : null,
            timeZoneOffsetMinutes: reminderEnabled ? -new Date().getTimezoneOffset() : null,
        };

        try {
            this.dialogRef.close(await this.data.saveGoalAsync(payload));
        } catch {
            this.saveFailed.set(true);
        } finally {
            this.isSaving.set(false);
        }
    }

    private normalizeTime(value: string | null | undefined): string {
        return value?.slice(0, DEFAULT_REMINDER_TIME.length) ?? DEFAULT_REMINDER_TIME;
    }
}

import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, max, min, required, validate } from '@angular/forms/signals';
import { finalize, firstValueFrom } from 'rxjs';

import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import {
    type CreateCyclePayload,
    CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
    CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS,
    CYCLE_REPRODUCTIVE_STATE_CYCLING,
    CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CycleConsentPurpose,
    type CycleResponse,
    type UpdateCycleSettingsPayload,
} from '../../../shared/models/cycle.data';
import { CyclesService } from '../api/cycles.service';
import {
    DEFAULT_AVERAGE_CYCLE_LENGTH,
    DEFAULT_AVERAGE_PERIOD_LENGTH,
    DEFAULT_LUTEAL_LENGTH,
    MAX_AVERAGE_CYCLE_LENGTH,
    MAX_AVERAGE_PERIOD_LENGTH,
    MAX_LUTEAL_LENGTH,
    MIN_AVERAGE_CYCLE_LENGTH,
    MIN_AVERAGE_PERIOD_LENGTH,
    MIN_LUTEAL_LENGTH,
} from './cycle-tracking.config';
import type { CycleSettingsFormModel, StartCycleFormModel } from './cycle-tracking.form-models';
import { toCycleDateKey } from './cycle-tracking.mapper';
import { CycleTrackingStateFacade } from './cycle-tracking-state.facade';

@Injectable()
export class CycleSettingsFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly state = inject(CycleTrackingStateFacade);
    public readonly startCycleModel = signal<StartCycleFormModel>({
        trackingStartDate: formatDateInputValue(new Date()),
        mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
        averageCycleLength: DEFAULT_AVERAGE_CYCLE_LENGTH,
        averagePeriodLength: DEFAULT_AVERAGE_PERIOD_LENGTH,
        lutealLength: DEFAULT_LUTEAL_LENGTH,
        isRegular: false,
        showFertilityEstimates: false,
        discreetNotifications: true,
        goal: CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
        reproductiveState: CYCLE_REPRODUCTIVE_STATE_CYCLING,
        hideFromDashboard: false,
        cycleTrackingConsentGranted: false,
        nutritionInsightsConsentGranted: false,
        fertilitySignalsConsentGranted: false,
    });

    private readonly submitStartCycleFormAsync = async (): Promise<void> => {
        await this.startCycleAsync();
    };

    public readonly startCycleForm = form(
        this.startCycleModel,
        path => {
            required(path.trackingStartDate);
            required(path.mode);
            required(path.goal);
            required(path.reproductiveState);
            required(path.cycleTrackingConsentGranted);
            validate(path.averageCycleLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            min(path.averageCycleLength, MIN_AVERAGE_CYCLE_LENGTH);
            max(path.averageCycleLength, MAX_AVERAGE_CYCLE_LENGTH);
            validate(path.averagePeriodLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            min(path.averagePeriodLength, MIN_AVERAGE_PERIOD_LENGTH);
            max(path.averagePeriodLength, MAX_AVERAGE_PERIOD_LENGTH);
            validate(path.lutealLength, ({ value }) => (value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' }));
            min(path.lutealLength, MIN_LUTEAL_LENGTH);
            max(path.lutealLength, MAX_LUTEAL_LENGTH);
        },
        {
            submission: {
                action: this.submitStartCycleFormAsync,
            },
        },
    );

    public readonly settingsModel = signal<CycleSettingsFormModel>({
        mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
        averageCycleLength: DEFAULT_AVERAGE_CYCLE_LENGTH,
        averagePeriodLength: DEFAULT_AVERAGE_PERIOD_LENGTH,
        lutealLength: DEFAULT_LUTEAL_LENGTH,
        isRegular: false,
        showFertilityEstimates: false,
        discreetNotifications: true,
        goal: CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
        reproductiveState: CYCLE_REPRODUCTIVE_STATE_CYCLING,
        hideFromDashboard: false,
        cycleTrackingConsentGranted: true,
        nutritionInsightsConsentGranted: false,
        fertilitySignalsConsentGranted: false,
    });

    private readonly submitSettingsFormAsync = async (): Promise<void> => {
        await this.saveSettingsAsync();
    };

    public readonly settingsForm = form(
        this.settingsModel,
        path => {
            disabled(path, { when: () => this.state.isSettingsBusy() });
            required(path.mode);
            required(path.goal);
            required(path.reproductiveState);
            validate(path.averageCycleLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            required(path.averageCycleLength);
            min(path.averageCycleLength, MIN_AVERAGE_CYCLE_LENGTH);
            max(path.averageCycleLength, MAX_AVERAGE_CYCLE_LENGTH);
            validate(path.averagePeriodLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            required(path.averagePeriodLength);
            min(path.averagePeriodLength, MIN_AVERAGE_PERIOD_LENGTH);
            max(path.averagePeriodLength, MAX_AVERAGE_PERIOD_LENGTH);
            validate(path.lutealLength, ({ value }) => (value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' }));
            required(path.lutealLength);
            min(path.lutealLength, MIN_LUTEAL_LENGTH);
            max(path.lutealLength, MAX_LUTEAL_LENGTH);
        },
        { submission: { action: this.submitSettingsFormAsync } },
    );

    public cancelSettingsEdit(): void {
        if (this.state.isSettingsBusy()) {
            return;
        }
        this.state.settingsError.set(null);
        const cycle = this.state.cycle();
        if (cycle === null) {
            return;
        }
        this.settingsForm().reset({
            mode: cycle.mode,
            averageCycleLength: cycle.averageCycleLength,
            averagePeriodLength: cycle.averagePeriodLength,
            lutealLength: cycle.lutealLength,
            isRegular: cycle.isRegular,
            showFertilityEstimates: cycle.showFertilityEstimates,
            discreetNotifications: cycle.discreetNotifications,
            goal: cycle.goal,
            reproductiveState: cycle.reproductiveState,
            hideFromDashboard: cycle.hideFromDashboard,
            cycleTrackingConsentGranted: true,
            nutritionInsightsConsentGranted: this.state.hasActiveConsent(cycle, CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS),
            fertilitySignalsConsentGranted: this.state.hasActiveConsent(cycle, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS),
        });
    }

    public startCycle(): void {
        void this.startCycleAsync();
    }

    private async startCycleAsync(): Promise<void> {
        if (this.startCycleForm().invalid()) {
            this.startCycleForm().markAsTouched();
            return;
        }

        const formValue = this.startCycleModel();
        if (formValue.trackingStartDate === null || formValue.trackingStartDate.length === 0) {
            return;
        }

        const payload: CreateCyclePayload = {
            trackingStartDate: toCycleDateKey(formValue.trackingStartDate),
            mode: formValue.mode ?? CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: formValue.averageCycleLength ?? undefined,
            averagePeriodLength: formValue.averagePeriodLength ?? undefined,
            lutealLength: formValue.lutealLength ?? undefined,
            isRegular: formValue.isRegular,
            isOnboardingComplete: true,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: formValue.discreetNotifications,
            goal: formValue.goal ?? CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
            reproductiveState: formValue.reproductiveState ?? CYCLE_REPRODUCTIVE_STATE_CYCLING,
            hideFromDashboard: formValue.hideFromDashboard,
            cycleTrackingConsentGranted: formValue.cycleTrackingConsentGranted,
            nutritionInsightsConsentGranted: formValue.nutritionInsightsConsentGranted,
            fertilitySignalsConsentGranted: formValue.fertilitySignalsConsentGranted,
        };

        this.state.isSavingCycle.set(true);
        const cycle = await firstValueFrom(
            this.cyclesService
                .create(payload)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .pipe(
                    finalize(() => {
                        this.state.isSavingCycle.set(false);
                    }),
                ),
        );
        this.state.cycle.set(cycle);
        this.state.loadNutritionSummary(cycle);
    }

    private async saveSettingsAsync(): Promise<void> {
        const currentCycle = this.state.cycle();
        if (this.state.isSettingsBusy()) {
            return;
        }
        if (currentCycle === null || this.settingsForm().invalid()) {
            this.settingsForm().markAsTouched();
            return;
        }

        const formValue = this.settingsModel();
        const payload = this.buildSettingsPayload(formValue);
        if (payload === null) {
            return;
        }

        this.state.settingsError.set(null);
        this.state.isSavingSettings.set(true);
        try {
            let updatedCycle = await firstValueFrom(
                this.cyclesService.updateSettings(currentCycle.id, payload).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.state.cycle.set(updatedCycle);
            updatedCycle = await this.updateConsentIfChangedAsync(
                updatedCycle,
                CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS,
                formValue.nutritionInsightsConsentGranted,
            );
            this.state.cycle.set(updatedCycle);
            updatedCycle = await this.updateConsentIfChangedAsync(
                updatedCycle,
                CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
                formValue.fertilitySignalsConsentGranted,
            );

            this.state.cycle.set(updatedCycle);
            this.state.loadNutritionSummary(updatedCycle);
            this.state.settingsSaveRevision.update(revision => revision + 1);
        } catch {
            this.state.settingsError.set('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
            this.state.loadNutritionSummary(this.state.cycle());
        } finally {
            this.state.isSavingSettings.set(false);
        }
    }

    private buildSettingsPayload(formValue: CycleSettingsFormModel): UpdateCycleSettingsPayload | null {
        if (
            formValue.mode === null ||
            formValue.averageCycleLength === null ||
            formValue.averagePeriodLength === null ||
            formValue.lutealLength === null
        ) {
            return null;
        }

        return {
            mode: formValue.mode,
            averageCycleLength: formValue.averageCycleLength,
            averagePeriodLength: formValue.averagePeriodLength,
            lutealLength: formValue.lutealLength,
            isRegular: formValue.isRegular,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: formValue.discreetNotifications,
            goal: formValue.goal ?? undefined,
            reproductiveState: formValue.reproductiveState ?? undefined,
            hideFromDashboard: formValue.hideFromDashboard,
        };
    }

    private async updateConsentIfChangedAsync(
        cycle: CycleResponse,
        purpose: CycleConsentPurpose,
        granted: boolean,
    ): Promise<CycleResponse> {
        if (this.state.hasActiveConsent(cycle, purpose) === granted) {
            return cycle;
        }

        return firstValueFrom(this.cyclesService.updateConsent(cycle.id, purpose, { granted }).pipe(takeUntilDestroyed(this.destroyRef)));
    }
}

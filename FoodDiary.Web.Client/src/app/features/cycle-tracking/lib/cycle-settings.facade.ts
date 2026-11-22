import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { disabled, form, max, min, required, submit, validate } from '@angular/forms/signals';
import { concatMap, filter, firstValueFrom, type Observable, of, take, takeUntil, tap } from 'rxjs';

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
    private readonly cycleProfileId = computed(() => this.state.cycle()?.id);
    private readonly cycleProfileChanges = toObservable(this.cycleProfileId);
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
        void submit(this.startCycleForm);
    }

    private async startCycleAsync(): Promise<void> {
        const formValue = this.startCycleModel();
        const payload: CreateCyclePayload = {
            trackingStartDate: toCycleDateKey(formValue.trackingStartDate),
            mode: formValue.mode,
            averageCycleLength: formValue.averageCycleLength ?? undefined,
            averagePeriodLength: formValue.averagePeriodLength ?? undefined,
            lutealLength: formValue.lutealLength ?? undefined,
            isRegular: formValue.isRegular,
            isOnboardingComplete: true,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: formValue.discreetNotifications,
            goal: formValue.goal,
            reproductiveState: formValue.reproductiveState,
            hideFromDashboard: formValue.hideFromDashboard,
            cycleTrackingConsentGranted: formValue.cycleTrackingConsentGranted,
            nutritionInsightsConsentGranted: formValue.nutritionInsightsConsentGranted,
            fertilitySignalsConsentGranted: formValue.fertilitySignalsConsentGranted,
        };

        this.state.createError.set(null);
        this.state.isSavingCycle.set(true);
        try {
            const cycle = await firstValueFrom(this.cyclesService.create(payload).pipe(takeUntilDestroyed(this.destroyRef)));
            this.state.cycle.set(cycle);
            this.state.loadNutritionSummary(cycle);
        } catch {
            this.state.createError.set('CYCLE_TRACKING.CREATE_CYCLE_FAILED');
        } finally {
            this.state.isSavingCycle.set(false);
        }
    }

    private async saveSettingsAsync(): Promise<void> {
        const currentCycle = this.state.cycle();
        if (this.state.isProfileMutationPending()) {
            return;
        }

        const formValue = this.settingsModel();
        const { averageCycleLength, averagePeriodLength, lutealLength } = formValue;
        if (currentCycle === null || averageCycleLength === null || averagePeriodLength === null || lutealLength === null) {
            return;
        }
        const payload: Required<UpdateCycleSettingsPayload> = {
            mode: formValue.mode,
            averageCycleLength,
            averagePeriodLength,
            lutealLength,
            isRegular: formValue.isRegular,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: formValue.discreetNotifications,
            goal: formValue.goal,
            reproductiveState: formValue.reproductiveState,
            hideFromDashboard: formValue.hideFromDashboard,
        };

        this.state.settingsError.set(null);
        this.state.isSavingSettings.set(true);
        const ownsProfile = (): boolean => this.cycleProfileId() === currentCycle.id && !this.destroyRef.destroyed;
        const profileChanged = this.cycleProfileChanges.pipe(filter(() => !ownsProfile()));
        const applyOwnedWrite = (request: Observable<CycleResponse>): Observable<CycleResponse> =>
            request.pipe(
                take(1),
                filter(ownsProfile),
                tap(cycle => {
                    this.state.acceptProfileWrite(cycle);
                }),
            );
        const updateConsentIfChanged = (cycle: CycleResponse, purpose: CycleConsentPurpose, granted: boolean): Observable<CycleResponse> =>
            this.state.hasActiveConsent(cycle, purpose) === granted
                ? of(cycle)
                : applyOwnedWrite(this.cyclesService.updateConsent(cycle.id, purpose, { granted }));
        try {
            const settings = this.hasSettingsChanges(currentCycle, payload)
                ? applyOwnedWrite(this.cyclesService.updateSettings(currentCycle.id, payload))
                : of(currentCycle);
            const updatedCycle = await firstValueFrom(
                settings.pipe(
                    concatMap(cycle =>
                        updateConsentIfChanged(cycle, CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS, formValue.nutritionInsightsConsentGranted),
                    ),
                    concatMap(cycle =>
                        updateConsentIfChanged(cycle, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS, formValue.fertilitySignalsConsentGranted),
                    ),
                    takeUntil(profileChanged),
                    takeUntilDestroyed(this.destroyRef),
                ),
            );
            if (!ownsProfile()) {
                return;
            }
            this.state.loadNutritionSummary(updatedCycle);
            this.state.settingsSaveRevision.update(revision => revision + 1);
        } catch {
            if (ownsProfile()) {
                this.state.settingsError.set('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
                this.state.loadNutritionSummary(this.state.cycle());
            }
        } finally {
            this.state.isSavingSettings.set(false);
        }
    }

    private hasSettingsChanges(cycle: CycleResponse, payload: Required<UpdateCycleSettingsPayload>): boolean {
        // The server derives the legacy mode from the explicit goal and reproductive state.
        const persisted: Readonly<Record<string, unknown>> = cycle;
        const requested: Array<[string, unknown]> = Object.entries(payload);
        return requested.some(([field, value]) => field !== 'mode' && value !== persisted[field]);
    }
}

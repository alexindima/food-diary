import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Subscription } from 'rxjs';

import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { RequestStateController } from '../../../shared/lib/request-state';
import {
    type BleedingEntry,
    CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
    type CycleConsentPurpose,
    type CycleFactor,
    type CycleNutritionSummary,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
    type FertilitySignal,
    type MenstrualEpisode,
} from '../../../shared/models/cycle.data';
import { CyclesService } from '../api/cycles.service';
import { cycleNutritionRange } from './cycle-nutrition-range';
import { toCycleDateKey } from './cycle-tracking.mapper';

@Injectable()
export class CycleTrackingStateFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly cycleLoad = new RequestStateController<true>();
    private readonly nutritionLoad = new RequestStateController<CycleNutritionSummary | null>();
    private readonly refreshError = signal<string | null>(null);
    private cycleRead: Subscription | undefined;
    private nutritionRead: Subscription | undefined;
    public readonly isLoading = this.cycleLoad.isLoading;

    public readonly loadError = computed(() => this.cycleLoad.error() ?? this.refreshError());

    public readonly isSavingCycle = signal(false);

    public readonly isSavingSettings = signal(false);

    public readonly isDeletingCycle = signal(false);

    public readonly isSettingsBusy = computed(() => this.isSavingSettings() || this.isDeletingCycle());

    public readonly isSavingDay = signal(false);

    public readonly isSavingFactor = signal(false);

    public readonly isSavingEpisode = signal(false);

    public readonly excludingEpisodeId = signal<string | null>(null);

    public readonly deletingEpisodeId = signal<string | null>(null);

    public readonly episodeError = signal<string | null>(null);

    public readonly isEpisodeBusy = computed(
        () =>
            this.isSavingEpisode() ||
            this.excludingEpisodeId() !== null ||
            this.deletingEpisodeId() !== null ||
            this.confirmingPeriodStartDate() !== null,
    );

    public readonly isExportingCycle = signal(false);

    public readonly exportError = signal<string | null>(null);

    public readonly daySaveRevision = signal(0);

    public readonly dayError = signal<string | null>(null);

    public readonly settingsSaveRevision = signal(0);

    public readonly settingsError = signal<string | null>(null);

    public readonly factorError = signal<string | null>(null);

    public readonly clearingDayDate = signal<string | null>(null);

    public readonly dayClearError = signal<string | null>(null);

    public readonly confirmingPeriodStartDate = signal<string | null>(null);

    public readonly periodStartError = signal<string | null>(null);

    public readonly isDayMutationPending = computed(
        () => this.isSavingDay() || this.clearingDayDate() !== null || this.confirmingPeriodStartDate() !== null,
    );

    public readonly editingDayDate = signal<string | null>(null);

    public readonly editingFactorId = signal<string | null>(null);

    public readonly editingEpisodeId = signal<string | null>(null);

    public readonly isLoadingNutritionSummary = this.nutritionLoad.isLoading;

    public readonly cycle = signal<CycleResponse | null>(null);

    public readonly nutritionSummary = this.nutritionLoad.data;
    public readonly nutritionError = this.nutritionLoad.error;

    public readonly predictions = computed<CyclePredictions | null>(() => this.cycle()?.predictions ?? null);

    public readonly bleedingEntries = computed<BleedingEntry[]>(() => [...(this.cycle()?.bleedingEntries ?? [])]);

    public readonly symptoms = computed<CycleSymptomEntry[]>(() => [...(this.cycle()?.symptoms ?? [])]);

    public readonly factors = computed<CycleFactor[]>(() => [...(this.cycle()?.factors ?? [])]);

    public readonly fertilitySignals = computed<FertilitySignal[]>(() => [...(this.cycle()?.fertilitySignals ?? [])]);

    public readonly menstrualEpisodes = computed<MenstrualEpisode[]>(() =>
        [...(this.cycle()?.menstrualEpisodes ?? [])].sort((left, right) => right.startDate.localeCompare(left.startDate)),
    );

    public readonly fertilityConsentGranted = computed(() => this.hasActiveConsent(this.cycle(), CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS));

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.cycleLoad.reset();
            this.nutritionLoad.reset();
        });
    }

    public loadNutritionSummary(cycle: CycleResponse | null): void {
        this.nutritionRead?.unsubscribe();
        if (cycle === null) {
            this.nutritionLoad.reset();
            return;
        }

        const requestId = this.nutritionLoad.begin();
        const range = cycleNutritionRange(toCycleDateKey(cycle.trackingStartDate), formatDateInputValue(new Date()));
        this.nutritionRead = this.cyclesService
            .getNutritionSummary(range.dateFrom, range.dateTo)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: summary => {
                    if (this.cycle()?.id === cycle.id) {
                        this.nutritionLoad.succeed(requestId, summary);
                    }
                },
                error: () => this.nutritionLoad.fail(requestId, 'CYCLE_TRACKING.LOAD_FAILED', { preserveData: false }),
            });
    }

    public loadCycle(onLoaded: () => void): void {
        if (this.isLoading()) {
            return;
        }
        this.cycleRead?.unsubscribe();
        this.refreshError.set(null);
        const requestId = this.cycleLoad.begin();
        this.cycleRead = this.cyclesService
            .getCurrent()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: cycle => {
                    if (!this.cycleLoad.succeed(requestId, true)) {
                        return;
                    }
                    this.cycle.set(cycle);
                    if (cycle !== null) {
                        onLoaded();
                    }
                    this.loadNutritionSummary(cycle);
                },
                error: () => {
                    this.cycleLoad.fail(requestId, 'CYCLE_TRACKING.LOAD_FAILED');
                },
            });
    }

    public clearCycle(): void {
        this.cycleRead?.unsubscribe();
        this.cycleLoad.reset();
        this.cycle.set(null);
        this.loadNutritionSummary(null);
    }

    public setRefreshError(error: string | null): void {
        this.refreshError.set(error);
    }

    public hasActiveConsent(cycle: CycleResponse | null, purpose: CycleConsentPurpose): boolean {
        return cycle?.consents?.some(consent => consent.purpose === purpose && consent.isActive) ?? false;
    }
}

import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { type Observable, of, switchMap, tap } from 'rxjs';

import { RequestStateController } from '../../../shared/lib/request-state';
import {
    type BleedingEntry,
    CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
    CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS,
    type CycleConsentPurpose,
    type CycleFactor,
    type CycleNutritionSummary,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
    type FertilitySignal,
    type MenstrualEpisode,
} from '../../../shared/models/cycle.data';
import { type CalendarDate, calendarDate } from '../../../shared/models/semantics/date-value';
import type { CycleFactorId, MenstrualEpisodeId } from '../../../shared/models/semantics/entity-id';
import { CyclesService } from '../api/cycles.service';
import { cycleNutritionRange } from './cycle-nutrition-range';
import { toCycleDateKey } from './cycle-tracking.mapper';

@Injectable()
export class CycleTrackingStateFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly cycleLoad = new RequestStateController<true>();
    private readonly nutritionLoad = new RequestStateController<CycleNutritionSummary | null>();
    private readonly cycleValue = signal<CycleResponse | null>(null);
    private readonly refreshError = signal<string | null>(null);
    private profileWriteRevision = 0;
    private profileReadRevision = 0;
    private latestFullReadRevision = 0;
    private readonly requestedCycle = signal<{ id: number; onLoaded: (cycle: CycleResponse | null) => void } | undefined>(undefined);
    private readonly cycleResource = rxResource({
        params: this.requestedCycle,
        stream: ({ params }) =>
            this.readCurrentCycle().pipe(
                tap({
                    next: ({ cycle, revision }) => {
                        if (this.cycleLoad.succeed(params.id, true)) {
                            this.latestFullReadRevision = revision;
                            this.refreshError.set(null);
                            this.publishProfileSnapshot(cycle);
                            params.onLoaded(cycle);
                            this.refreshNutritionSummary();
                        }
                    },
                    error: () => {
                        this.cycleLoad.fail(params.id, 'CYCLE_TRACKING.LOAD_FAILED');
                    },
                }),
            ),
    });
    private readonly requestedNutrition = signal<{ id: number; from: CalendarDate; to: CalendarDate } | undefined>(undefined);
    private readonly nutritionResource = rxResource({
        params: this.requestedNutrition,
        stream: ({ params: { id, from, to } }) =>
            this.cyclesService.getNutritionSummary(from, to).pipe(
                tap({
                    next: summary => this.nutritionLoad.succeed(id, summary),
                    error: () => this.nutritionLoad.fail(id, 'CYCLE_TRACKING.NUTRITION_SUMMARY_LOAD_ERROR', { preserveData: false }),
                }),
            ),
    });
    public readonly isLoading = this.cycleLoad.isLoading;

    public readonly loadError = computed(() => this.cycleLoad.error() ?? this.refreshError());

    public readonly isSavingCycle = signal(false);

    public readonly createError = signal<string | null>(null);

    public readonly isSavingSettings = signal(false);

    public readonly isDeletingCycle = signal(false);

    public readonly isSettingsBusy = computed(() => this.isSavingSettings() || this.isDeletingCycle());

    public readonly isSavingDay = signal(false);

    public readonly isSavingFactor = signal(false);

    public readonly isSavingEpisode = signal(false);

    public readonly excludingEpisodeId = signal<MenstrualEpisodeId | null>(null);

    public readonly deletingEpisodeId = signal<MenstrualEpisodeId | null>(null);

    public readonly episodeError = signal<string | null>(null);

    private readonly isEpisodeWritePending = computed(
        () => this.isSavingEpisode() || this.excludingEpisodeId() !== null || this.deletingEpisodeId() !== null,
    );

    public readonly isEpisodeBusy = computed(() => this.isEpisodeWritePending() || this.confirmingPeriodStartDate() !== null);

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

    public readonly isProfileMutationPending = computed(
        () => this.isSettingsBusy() || this.isDayMutationPending() || this.isSavingFactor() || this.isEpisodeWritePending(),
    );

    public readonly editingDayDate = signal<string | null>(null);

    public readonly editingFactorId = signal<CycleFactorId | null>(null);

    public readonly editingEpisodeId = signal<MenstrualEpisodeId | null>(null);

    public readonly isLoadingNutritionSummary = this.nutritionLoad.isLoading;

    public readonly cycle = this.cycleValue.asReadonly();

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
            this.resetNutritionSummary();
        });
    }

    public acceptProfileWrite(cycle: CycleResponse): void {
        this.profileWriteRevision += 1;
        this.publishProfileSnapshot(cycle);
    }

    public publishProfileSnapshot(cycle: CycleResponse | null): void {
        const previous = this.cycleValue();
        const nutritionPurpose = CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS;
        if (
            previous?.id !== cycle?.id ||
            (this.hasActiveConsent(previous, nutritionPurpose) && !this.hasActiveConsent(cycle, nutritionPurpose))
        ) {
            this.resetNutritionSummary();
        }
        this.cycleValue.set(cycle);
    }

    public startProfileRead(): { revision: number; response: Observable<CycleResponse | null> } {
        return { revision: ++this.profileReadRevision, response: this.cyclesService.getCurrent() };
    }

    public hasNewerFullRead(revision: number): boolean {
        return this.latestFullReadRevision > revision;
    }

    public refreshNutritionSummary(): void {
        const cycle = this.cycleValue();
        if (cycle === null) {
            this.resetNutritionSummary();
            return;
        }

        const range = cycleNutritionRange(toCycleDateKey(cycle.trackingStartDate), new Date());
        if (range.dateFrom > range.dateTo) {
            this.resetNutritionSummary();
            return;
        }

        this.nutritionResource.set(undefined);
        this.requestedNutrition.set({
            id: this.nutritionLoad.begin(),
            from: calendarDate(range.dateFrom),
            to: calendarDate(range.dateTo),
        });
    }

    private resetNutritionSummary(): void {
        this.nutritionLoad.reset();
        this.requestedNutrition.set(undefined);
        this.nutritionResource.set(undefined);
    }

    public loadCycle(onLoaded: (cycle: CycleResponse | null) => void): void {
        if (this.isLoading()) {
            return;
        }
        this.refreshError.set(null);
        this.requestedCycle.set({ id: this.cycleLoad.begin(), onLoaded });
    }

    private readCurrentCycle(): Observable<{ cycle: CycleResponse | null; revision: number }> {
        const profileId = this.cycle()?.id;
        const writeRevision = this.profileWriteRevision;
        const { revision, response } = this.startProfileRead();
        return response.pipe(
            switchMap(cycle => {
                if (cycle?.id === profileId && this.cycle()?.id === profileId && writeRevision !== this.profileWriteRevision) {
                    // Read again after a write made while this profile snapshot was in flight.
                    return this.readCurrentCycle();
                }
                return of({ cycle, revision });
            }),
        );
    }

    public clearCycle(): void {
        this.cycleLoad.reset();
        this.requestedCycle.set(undefined);
        this.cycleResource.set(undefined);
        this.publishProfileSnapshot(null);
    }

    public setRefreshError(error: string | null): void {
        this.refreshError.set(error);
    }

    public hasActiveConsent(cycle: CycleResponse | null, purpose: CycleConsentPurpose): boolean {
        return cycle?.consents?.some(consent => consent.purpose === purpose && consent.isActive) ?? false;
    }
}

import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import type { CycleFactorId, MenstrualEpisodeId } from '../../../shared/models/semantics/entity-id';
import { CyclesService } from '../api/cycles.service';
import { CycleDayFacade } from './cycle-day.facade';
import { CycleEpisodeFacade } from './cycle-episode.facade';
import { CycleExportFacade } from './cycle-export.facade';
import type { CycleExportRange } from './cycle-export-range';
import { CycleFactorFacade } from './cycle-factor.facade';
import { CycleSettingsFacade } from './cycle-settings.facade';
import { CycleTrackingStateFacade } from './cycle-tracking-state.facade';
export type { CycleDayFormModel, CycleSettingsFormModel } from './cycle-tracking.form-models';

@Injectable()
export class CycleTrackingFacade {
    private readonly destroyRef = inject(DestroyRef);
    private readonly cyclesService = inject(CyclesService);
    private readonly state = inject(CycleTrackingStateFacade);
    private readonly settings = inject(CycleSettingsFacade);
    private readonly day = inject(CycleDayFacade);
    private readonly factor = inject(CycleFactorFacade);
    private readonly episode = inject(CycleEpisodeFacade);
    private readonly export = inject(CycleExportFacade);
    public readonly isLoading = this.state.isLoading;
    public readonly loadError = this.state.loadError;
    public readonly isSavingCycle = this.state.isSavingCycle;
    public readonly isSavingSettings = this.state.isSavingSettings;
    public readonly isDeletingCycle = this.state.isDeletingCycle;
    public readonly isSavingDay = this.state.isSavingDay;
    public readonly isSavingFactor = this.state.isSavingFactor;
    public readonly isSavingEpisode = this.state.isSavingEpisode;
    public readonly excludingEpisodeId = this.state.excludingEpisodeId;
    public readonly deletingEpisodeId = this.state.deletingEpisodeId;
    public readonly episodeError = this.state.episodeError;
    public readonly isEpisodeBusy = this.state.isEpisodeBusy;
    public readonly isExportingCycle = this.state.isExportingCycle;
    public readonly exportError = this.state.exportError;
    public readonly daySaveRevision = this.state.daySaveRevision;
    public readonly dayError = this.state.dayError;
    public readonly settingsSaveRevision = this.state.settingsSaveRevision;
    public readonly settingsError = this.state.settingsError;
    public readonly factorError = this.state.factorError;
    public readonly clearingDayDate = this.state.clearingDayDate;
    public readonly dayClearError = this.state.dayClearError;
    public readonly confirmingPeriodStartDate = this.state.confirmingPeriodStartDate;
    public readonly periodStartError = this.state.periodStartError;
    public readonly editingDayDate = this.state.editingDayDate;
    public readonly editingFactorId = this.state.editingFactorId;
    public readonly editingEpisodeId = this.state.editingEpisodeId;
    public readonly isLoadingNutritionSummary = this.state.isLoadingNutritionSummary;
    public readonly cycle = this.state.cycle;
    public readonly nutritionSummary = this.state.nutritionSummary;
    public readonly startCycleModel = this.settings.startCycleModel;
    public readonly startCycleForm = this.settings.startCycleForm;
    public readonly settingsModel = this.settings.settingsModel;
    public readonly settingsForm = this.settings.settingsForm;
    public readonly dayModel = this.day.dayModel;
    public readonly dayForm = this.day.dayForm;
    public readonly factorModel = this.factor.factorModel;
    public readonly factorForm = this.factor.factorForm;
    public readonly episodeModel = this.episode.episodeModel;
    public readonly episodeForm = this.episode.episodeForm;
    public readonly predictions = this.state.predictions;
    public readonly bleedingEntries = this.state.bleedingEntries;
    public readonly symptoms = this.state.symptoms;
    public readonly factors = this.state.factors;
    public readonly fertilitySignals = this.state.fertilitySignals;
    public readonly menstrualEpisodes = this.state.menstrualEpisodes;
    public readonly fertilityConsentGranted = this.state.fertilityConsentGranted;
    public cancelSettingsEdit(): void {
        this.settings.cancelSettingsEdit();
    }
    public startCycle(): void {
        this.settings.startCycle();
    }
    public saveDay(): void {
        this.day.saveDay();
    }
    public editDay(date: string): void {
        this.day.editDay(date);
    }
    public cancelDayEdit(): void {
        this.day.cancelDayEdit();
    }
    public saveFactor(): void {
        this.factor.saveFactor();
    }
    public editFactor(factorId: CycleFactorId): void {
        this.factor.editFactor(factorId);
    }
    public cancelFactorEdit(): void {
        this.factor.cancelFactorEdit();
    }
    public endFactorToday(factorId: CycleFactorId): void {
        this.factor.endFactorToday(factorId);
    }
    public async endFactorTodayAsync(factorId: CycleFactorId): Promise<void> {
        return this.factor.endFactorTodayAsync(factorId);
    }
    public clearDay(date: string): void {
        this.day.clearDay(date);
    }
    public confirmPeriodStart(date: string): void {
        this.day.confirmPeriodStart(date);
    }
    public editMenstrualEpisode(episodeId: MenstrualEpisodeId): void {
        this.episode.editMenstrualEpisode(episodeId);
    }
    public cancelMenstrualEpisodeEdit(): void {
        this.episode.cancelMenstrualEpisodeEdit();
    }
    public async toggleMenstrualEpisodePredictionAsync(episodeId: MenstrualEpisodeId): Promise<void> {
        return this.episode.toggleMenstrualEpisodePredictionAsync(episodeId);
    }
    public async deleteMenstrualEpisodeAsync(episodeId: MenstrualEpisodeId): Promise<void> {
        return this.episode.deleteMenstrualEpisodeAsync(episodeId);
    }
    public exportCycle(range?: CycleExportRange): void {
        this.export.exportCycle(range);
    }
    public exportSensitiveCycle(currentPassword: string, range?: CycleExportRange): void {
        this.export.exportSensitiveCycle(currentPassword, range);
    }
    public initialize(): void {
        this.state.loadCycle(() => {
            this.settings.cancelSettingsEdit();
        });
    }
    public async deleteCycleAsync(): Promise<void> {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || this.state.isDeletingCycle() || this.state.isSavingSettings()) {
            return;
        }

        this.state.settingsError.set(null);
        this.state.isDeletingCycle.set(true);
        try {
            await firstValueFrom(this.cyclesService.deleteCycle(currentCycle.id).pipe(takeUntilDestroyed(this.destroyRef)));
            this.state.clearCycle();
            this.day.cancelDayEdit();
            this.factor.cancelFactorEdit();
            this.episode.cancelMenstrualEpisodeEdit();
        } catch {
            this.state.settingsError.set('CYCLE_TRACKING.DELETE_CYCLE_FAILED');
        } finally {
            this.state.isDeletingCycle.set(false);
        }
    }
}

import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, required, validate } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';

import { calendarDate } from '../../../shared/models/semantics/date-value';
import type { MenstrualEpisodeId } from '../../../shared/models/semantics/entity-id';
import { CyclesService } from '../api/cycles.service';
import type { MenstrualEpisodeFormModel } from './cycle-tracking.form-models';
import { toCycleDateKey } from './cycle-tracking.mapper';
import { CycleTrackingStateFacade } from './cycle-tracking-state.facade';

@Injectable()
export class CycleEpisodeFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly state = inject(CycleTrackingStateFacade);
    public readonly episodeModel = signal<MenstrualEpisodeFormModel>({
        startDate: null,
        endDate: null,
    });

    private readonly submitEpisodeFormAsync = async (): Promise<void> => {
        await this.saveMenstrualEpisodeAsync();
    };

    public readonly episodeForm = form(
        this.episodeModel,
        path => {
            required(path.startDate);
            disabled(path, { when: () => this.state.isEpisodeBusy() });
            validate(path.endDate, context => {
                const start = context.valueOf(path.startDate);
                const end = context.value();
                if (start === null || end === null || start.length === 0 || end.length === 0) {
                    return;
                }
                return toCycleDateKey(end) < toCycleDateKey(start) ? { kind: 'dateOrder' } : undefined;
            });
        },
        {
            submission: {
                action: this.submitEpisodeFormAsync,
            },
        },
    );

    public editMenstrualEpisode(episodeId: MenstrualEpisodeId): void {
        if (this.state.isEpisodeBusy()) {
            return;
        }
        const episode = this.state.menstrualEpisodes().find(item => item.id === episodeId);
        if (episode === undefined) {
            return;
        }

        this.state.episodeError.set(null);
        this.episodeModel.set({
            startDate: calendarDate(toCycleDateKey(episode.startDate)),
            endDate: episode.endDate === undefined || episode.endDate === null ? null : calendarDate(toCycleDateKey(episode.endDate)),
        });
        this.state.editingEpisodeId.set(episodeId);
    }

    public cancelMenstrualEpisodeEdit(): void {
        if (this.state.isEpisodeBusy()) {
            return;
        }
        this.state.episodeError.set(null);
        this.resetMenstrualEpisodeEdit();
    }

    private resetMenstrualEpisodeEdit(): void {
        this.state.editingEpisodeId.set(null);
        this.episodeModel.set({ startDate: null, endDate: null });
    }

    private async saveMenstrualEpisodeAsync(): Promise<void> {
        const currentCycle = this.state.cycle();
        const episodeId = this.state.editingEpisodeId();
        const formValue = this.episodeModel();
        if (
            currentCycle === null ||
            episodeId === null ||
            formValue.startDate === null ||
            formValue.startDate.length === 0 ||
            this.episodeForm().invalid() ||
            this.state.isEpisodeBusy()
        ) {
            this.episodeForm().markAsTouched();
            return;
        }

        this.state.episodeError.set(null);
        this.state.isSavingEpisode.set(true);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService
                    .updateMenstrualEpisode(currentCycle.id, episodeId, {
                        startDate: calendarDate(toCycleDateKey(formValue.startDate)),
                        endDate:
                            formValue.endDate === null || formValue.endDate.length === 0
                                ? null
                                : calendarDate(toCycleDateKey(formValue.endDate)),
                    })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.state.cycle.set(cycle);
            this.resetMenstrualEpisodeEdit();
        } catch {
            this.state.episodeError.set('CYCLE_TRACKING.EPISODE_SAVE_FAILED');
        } finally {
            this.state.isSavingEpisode.set(false);
        }
    }

    public async toggleMenstrualEpisodePredictionAsync(episodeId: MenstrualEpisodeId): Promise<void> {
        const currentCycle = this.state.cycle();
        const episode = this.state.menstrualEpisodes().find(item => item.id === episodeId);
        if (currentCycle === null || episode?.status !== 1 || this.hasPendingEpisodeAction()) {
            return;
        }

        this.state.episodeError.set(null);
        this.state.excludingEpisodeId.set(episodeId);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService
                    .updateMenstrualEpisode(currentCycle.id, episodeId, {
                        startDate: calendarDate(toCycleDateKey(episode.startDate)),
                        endDate:
                            episode.endDate === undefined || episode.endDate === null
                                ? null
                                : calendarDate(toCycleDateKey(episode.endDate)),
                        excludedFromPredictions: !episode.excludedFromPredictions,
                    })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.state.cycle.set(cycle);
        } catch {
            this.state.episodeError.set('CYCLE_TRACKING.EPISODE_PREDICTION_FAILED');
        } finally {
            this.state.excludingEpisodeId.set(null);
        }
    }

    public async deleteMenstrualEpisodeAsync(episodeId: MenstrualEpisodeId): Promise<void> {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || this.hasPendingEpisodeAction()) {
            return;
        }

        this.state.episodeError.set(null);
        this.state.deletingEpisodeId.set(episodeId);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService.deleteMenstrualEpisode(currentCycle.id, episodeId).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.state.cycle.set(cycle);
            if (this.state.editingEpisodeId() === episodeId) {
                this.resetMenstrualEpisodeEdit();
            }
        } catch {
            this.state.episodeError.set('CYCLE_TRACKING.EPISODE_DELETE_FAILED');
        } finally {
            this.state.deletingEpisodeId.set(null);
        }
    }

    private hasPendingEpisodeAction(): boolean {
        return this.state.isEpisodeBusy();
    }
}

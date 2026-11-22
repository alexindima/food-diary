import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, required, validate } from '@angular/forms/signals';
import { firstValueFrom, type Observable, Subject, takeUntil } from 'rxjs';

import type { CycleResponse } from '../../../shared/models/cycle.data';
import { type CalendarDate, calendarDate } from '../../../shared/models/semantics/date-value';
import type { CycleProfileId, MenstrualEpisodeId } from '../../../shared/models/semantics/entity-id';
import { CyclesService } from '../api/cycles.service';
import type { MenstrualEpisodeFormModel } from './cycle-tracking.form-models';
import { toCycleDateKey } from './cycle-tracking.mapper';
import { CycleTrackingStateFacade } from './cycle-tracking-state.facade';

@Injectable()
export class CycleEpisodeFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly state = inject(CycleTrackingStateFacade);
    private readonly profileChanged = new Subject<void>();
    private activeEpisodeAction: symbol | null = null;
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

    public clearAfterProfileChange(): void {
        this.clearEpisodeAction();
        this.profileChanged.next();
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
            this.state.isProfileMutationPending()
        ) {
            this.episodeForm().markAsTouched();
            return;
        }
        if (!this.state.menstrualEpisodes().some(episode => episode.id === episodeId)) {
            this.resetMenstrualEpisodeEdit();
            return;
        }

        const action = this.beginEpisodeAction();
        this.state.isSavingEpisode.set(true);
        try {
            const cycle = await firstValueFrom(
                this.trackEpisodeResponse(
                    this.cyclesService.updateMenstrualEpisode(currentCycle.id, episodeId, {
                        startDate: calendarDate(toCycleDateKey(formValue.startDate)),
                        endDate: episodeEndDate(formValue.endDate),
                    }),
                ),
                { defaultValue: null },
            );
            if (this.applyEpisodeResponse(cycle, action)) {
                this.resetMenstrualEpisodeEdit();
            }
        } catch {
            this.setEpisodeErrorForProfile(currentCycle.id, action, 'CYCLE_TRACKING.EPISODE_SAVE_FAILED');
        } finally {
            this.finishEpisodeAction(action);
        }
    }

    public async toggleMenstrualEpisodePredictionAsync(episodeId: MenstrualEpisodeId): Promise<void> {
        const currentCycle = this.state.cycle();
        const episode = this.state.menstrualEpisodes().find(item => item.id === episodeId);
        if (currentCycle === null || episode?.status !== 1 || this.state.isProfileMutationPending()) {
            return;
        }

        const action = this.beginEpisodeAction();
        this.state.excludingEpisodeId.set(episodeId);
        try {
            const cycle = await firstValueFrom(
                this.trackEpisodeResponse(
                    this.cyclesService.updateMenstrualEpisode(currentCycle.id, episodeId, {
                        startDate: calendarDate(toCycleDateKey(episode.startDate)),
                        endDate: episodeEndDate(episode.endDate),
                        excludedFromPredictions: !episode.excludedFromPredictions,
                    }),
                ),
                { defaultValue: null },
            );
            this.applyEpisodeResponse(cycle, action);
        } catch {
            this.setEpisodeErrorForProfile(currentCycle.id, action, 'CYCLE_TRACKING.EPISODE_PREDICTION_FAILED');
        } finally {
            this.finishEpisodeAction(action);
        }
    }

    public async deleteMenstrualEpisodeAsync(episodeId: MenstrualEpisodeId): Promise<void> {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || this.state.isProfileMutationPending()) {
            return;
        }

        const action = this.beginEpisodeAction();
        this.state.deletingEpisodeId.set(episodeId);
        try {
            const cycle = await firstValueFrom(
                this.trackEpisodeResponse(this.cyclesService.deleteMenstrualEpisode(currentCycle.id, episodeId)),
                {
                    defaultValue: null,
                },
            );
            if (this.applyEpisodeResponse(cycle, action) && this.state.editingEpisodeId() === episodeId) {
                this.resetMenstrualEpisodeEdit();
            }
        } catch {
            this.setEpisodeErrorForProfile(currentCycle.id, action, 'CYCLE_TRACKING.EPISODE_DELETE_FAILED');
        } finally {
            this.finishEpisodeAction(action);
        }
    }

    private beginEpisodeAction(): symbol {
        const action = Symbol();
        this.activeEpisodeAction = action;
        this.state.episodeError.set(null);
        return action;
    }

    private finishEpisodeAction(action: symbol): void {
        if (this.activeEpisodeAction === action) {
            this.clearEpisodeAction();
        }
    }

    private clearEpisodeAction(): void {
        this.activeEpisodeAction = null;
        this.state.isSavingEpisode.set(false);
        this.state.excludingEpisodeId.set(null);
        this.state.deletingEpisodeId.set(null);
    }

    private trackEpisodeResponse(response: Observable<CycleResponse>): Observable<CycleResponse> {
        return response.pipe(takeUntil(this.profileChanged), takeUntilDestroyed(this.destroyRef));
    }

    private applyEpisodeResponse(cycle: CycleResponse | null, action: symbol): boolean {
        if (cycle === null || !this.isCurrentEpisodeAction(cycle.id, action)) {
            return false;
        }
        this.state.acceptProfileWrite(cycle);
        return true;
    }

    private setEpisodeErrorForProfile(profileId: CycleProfileId, action: symbol, message: string): void {
        if (this.isCurrentEpisodeAction(profileId, action)) {
            this.state.episodeError.set(message);
        }
    }

    private isCurrentEpisodeAction(profileId: CycleProfileId, action: symbol): boolean {
        return this.activeEpisodeAction === action && this.state.cycle()?.id === profileId;
    }
}

function episodeEndDate(value: string | null | undefined): CalendarDate | null {
    return value === null || value === undefined || value.length === 0 ? null : calendarDate(toCycleDateKey(value));
}

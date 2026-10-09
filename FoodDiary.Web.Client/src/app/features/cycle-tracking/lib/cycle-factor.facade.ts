import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, required, validate } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';

import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { getRecordProperty, getStringProperty } from '../../../shared/lib/unknown-value.utils';
import { CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION, type UpsertCycleFactorPayload } from '../../../shared/models/cycle.data';
import { calendarDate } from '../../../shared/models/semantics/date-value';
import type { CycleFactorId } from '../../../shared/models/semantics/entity-id';
import { CyclesService } from '../api/cycles.service';
import { getCycleFactorStatus } from './cycle-factor-status.utils';
import { MAX_CYCLE_NOTES_LENGTH } from './cycle-tracking.config';
import type { CycleFactorFormModel } from './cycle-tracking.form-models';
import { getCycleNotesLength, toCycleDateKey, toOptionalCycleText } from './cycle-tracking.mapper';
import { CycleTrackingStateFacade } from './cycle-tracking-state.facade';

@Injectable()
export class CycleFactorFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly state = inject(CycleTrackingStateFacade);
    public readonly factorModel = signal<CycleFactorFormModel>({
        type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
        startDate: formatDateInputValue(new Date()),
        endDate: null,
        notes: null,
    });

    private readonly submitFactorFormAsync = async (): Promise<void> => {
        await this.saveFactorAsync();
    };

    public readonly factorForm = form(
        this.factorModel,
        path => {
            disabled(path, { when: () => this.state.isSavingFactor() });
            required(path.type);
            required(path.startDate);
            validate(path.notes, context => {
                // Match the trimmed payload and the server's Unicode whitespace trimming.
                return getCycleNotesLength(context.value()) > MAX_CYCLE_NOTES_LENGTH ? { kind: 'notesTooLong' } : undefined;
            });
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
                action: this.submitFactorFormAsync,
            },
        },
    );

    public saveFactor(): void {
        void this.saveFactorAsync();
    }

    private async saveFactorAsync(): Promise<void> {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.state.isSavingFactor()) {
            return;
        }

        if (this.factorForm().invalid()) {
            this.factorForm().markAsTouched();
            return;
        }

        const payload = this.buildFactorPayload(this.factorModel());
        if (payload === null) {
            return;
        }

        this.state.factorError.set(null);
        this.state.isSavingFactor.set(true);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService.upsertFactor(currentCycle.id, payload).pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.state.cycle.set(cycle);
            this.resetFactorForm();
            this.state.loadNutritionSummary(cycle);
        } catch (error: unknown) {
            this.state.factorError.set(
                getStringProperty(getRecordProperty(error, 'error'), 'error') === 'Cycle.FactorIdentityConflict'
                    ? 'CYCLE_TRACKING.FACTOR_IDENTITY_CONFLICT'
                    : 'CYCLE_TRACKING.SAVE_FACTOR_FAILED',
            );
        } finally {
            this.state.isSavingFactor.set(false);
        }
    }

    private buildFactorPayload(formValue: CycleFactorFormModel): UpsertCycleFactorPayload | null {
        if (formValue.type === null || formValue.startDate === null || formValue.startDate.length === 0) {
            return null;
        }
        const notes = toOptionalCycleText(formValue.notes);
        const factorId = this.state.editingFactorId();
        return {
            ...(factorId === null ? {} : { factorId }),
            type: formValue.type,
            startDate: calendarDate(toCycleDateKey(formValue.startDate)),
            endDate: formValue.endDate === null || formValue.endDate.length === 0 ? null : calendarDate(toCycleDateKey(formValue.endDate)),
            notes,
            clearNotes: this.state.editingFactorId() !== null && notes === undefined,
        };
    }

    public editFactor(factorId: CycleFactorId): void {
        if (this.state.isSavingFactor()) {
            return;
        }
        const factor = this.state.factors().find(item => item.id === factorId);
        if (factor === undefined) {
            return;
        }

        this.state.factorError.set(null);
        this.factorForm().reset({
            type: factor.type,
            startDate: calendarDate(toCycleDateKey(factor.startDate)),
            endDate: factor.endDate === null || factor.endDate === undefined ? null : calendarDate(toCycleDateKey(factor.endDate)),
            notes: factor.notes ?? null,
        });
        this.state.editingFactorId.set(factorId);
    }

    public cancelFactorEdit(): void {
        if (this.state.isSavingFactor()) {
            return;
        }
        this.state.factorError.set(null);
        this.resetFactorForm();
    }

    private resetFactorForm(): void {
        this.state.editingFactorId.set(null);
        this.factorForm().reset({
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: formatDateInputValue(new Date()),
            endDate: null,
            notes: null,
        });
    }

    public endFactorToday(factorId: CycleFactorId): void {
        void this.endFactorTodayAsync(factorId);
    }

    public async endFactorTodayAsync(factorId: CycleFactorId): Promise<void> {
        const currentCycle = this.state.cycle();
        const factor = this.state.factors().find(item => item.id === factorId);
        const today = formatDateInputValue(new Date());
        if (
            currentCycle === null ||
            factor === undefined ||
            this.state.isSavingFactor() ||
            !getCycleFactorStatus(factor, today).canEndToday
        ) {
            return;
        }

        this.state.factorError.set(null);
        this.state.isSavingFactor.set(true);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService
                    .upsertFactor(currentCycle.id, {
                        factorId: factor.id,
                        type: factor.type,
                        startDate: calendarDate(toCycleDateKey(factor.startDate)),
                        endDate: calendarDate(today),
                        notes: factor.notes ?? undefined,
                        clearNotes: false,
                    })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            this.state.cycle.set(cycle);
            if (this.state.editingFactorId() === factorId) {
                this.resetFactorForm();
            }
            this.state.loadNutritionSummary(cycle);
        } catch {
            this.state.factorError.set('CYCLE_TRACKING.END_FACTOR_FAILED');
        } finally {
            this.state.isSavingFactor.set(false);
        }
    }
}

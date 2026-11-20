import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiPreventUnloadDirective } from 'fd-ui-kit';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { UnsavedChangesBarComponent } from '../../../../components/shared/unsaved-changes-bar/unsaved-changes-bar';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../../services/unsaved-changes.service';
import { type DayCalorieKey, DAYS_OF_WEEK, type UpdateGoalsRequest } from '../../../../shared/models/goals.data';
import type { BodyTargetKey, MacroKey, MacroPreset, MacroPresetKey } from '../../lib/goals.facade';
import { createDayCalories } from '../../lib/goals-state.mapper';
import { GoalsCyclingRowComponent } from './goals-cycling-row';
import {
    applyMacroPreset,
    buildDraftRequest,
    calculateMacroPercent,
    type GoalNumberKey,
    type GoalsDraft,
    type GoalsMacroDraft,
    isGoalsDraftValid,
} from './goals-editor.models';
import { GoalsNutritionCardComponent } from './goals-nutrition-card';
import { GoalsSideCardsComponent } from './goals-side-cards';
import { GoalsSummaryCardComponent } from './goals-summary-card';

@Component({
    selector: 'fd-goals-editor',
    imports: [
        TranslatePipe,
        FdUiPreventUnloadDirective,
        UnsavedChangesBarComponent,
        GoalsSummaryCardComponent,
        GoalsNutritionCardComponent,
        GoalsSideCardsComponent,
        GoalsCyclingRowComponent,
    ],
    templateUrl: './goals-editor.html',
    styleUrl: './goals-editor.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsEditorComponent {
    private readonly destroyRef = inject(DestroyRef);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    public readonly calories = input.required<number>();
    public readonly macros = input.required<GoalsMacroDraft[]>();
    public readonly preset = input.required<MacroPresetKey>();
    public readonly presets = input.required<MacroPreset[]>();
    public readonly presetOptions = input.required<Array<FdUiSelectOption<MacroPresetKey>>>();
    public readonly water = input.required<number>();
    public readonly bodyTargets = input.required<Record<BodyTargetKey, number>>();
    public readonly cyclingEnabled = input.required<boolean>();
    public readonly dayCalories = input.required<Record<DayCalorieKey, number>>();
    public readonly saving = input(false);
    public readonly saveRequest = input.required<(request: UpdateGoalsRequest) => Promise<boolean>>();
    private pendingSave: Promise<boolean> | null = null;

    protected readonly draft = signal<GoalsDraft | null>(null);
    protected readonly dirty = signal(false);
    protected readonly draftValid = computed(() => isGoalsDraftValid(this.requireDraft()));
    protected readonly macroDrafts = computed(() => {
        const draft = this.draft();
        const calories = draft?.calories ?? this.calories();
        return this.macros().map(macro => {
            const value = draft?.macros[macro.key] ?? macro.value;
            return { ...macro, value, percent: calculateMacroPercent(macro.key, value, calories) };
        });
    });

    public constructor() {
        effect(() => {
            const source = this.sourceDraft();
            if (!untracked(this.dirty)) {
                this.draft.set(source);
            }
        });
        const handler: UnsavedChangesHandler = {
            hasChanges: () => this.dirty(),
            save: this.persistAsync.bind(this),
            discard: () => {
                this.discard();
            },
        };
        this.unsavedChangesService.register(handler);
        this.destroyRef.onDestroy(() => {
            this.unsavedChangesService.clear(handler);
        });
    }

    protected updateCalories(value: number | null): void {
        const draft = this.withNumberEdit('calories', value);
        if (value === null) {
            this.replaceDraft(draft);
            return;
        }
        const current = { ...draft, calories: value };
        const preset = this.presets().find(item => item.key === current.preset);
        this.replaceDraft(preset === undefined ? current : applyMacroPreset(current, preset));
    }

    protected updateMacro({ key, value }: { key: MacroKey; value: number | null }): void {
        const draft = this.withNumberEdit(key, value);
        const macros = value === null ? draft.macros : { ...draft.macros, [key]: value };
        this.replaceDraft({ ...draft, macros, preset: 'custom' });
    }

    protected updatePreset(value: MacroPresetKey): void {
        const preset = this.presets().find(item => item.key === value);
        this.replaceDraft(preset === undefined ? { ...this.requireDraft(), preset: value } : applyMacroPreset(this.requireDraft(), preset));
    }

    protected updateWater(value: number | null): void {
        const draft = this.withNumberEdit('water', value);
        this.replaceDraft({ ...draft, water: value ?? draft.water });
    }

    protected updateBodyTarget(change: { key: BodyTargetKey; value: number | null }): void {
        const current = this.requireDraft();
        this.updateDraft({
            bodyTargets: change.value === null ? current.bodyTargets : { ...current.bodyTargets, [change.key]: change.value },
            invalidBodyTargets: { ...current.invalidBodyTargets, [change.key]: change.value === null },
        });
    }

    protected updateCycling(enabled: boolean): void {
        const current = this.requireDraft();
        const shouldInitializeDays = enabled && current.calories > 0 && Object.values(current.dayCalories).every(value => value === 0);
        const incompleteNumbers = new Set(current.incompleteNumbers);
        if (!enabled) {
            for (const day of DAYS_OF_WEEK) {
                incompleteNumbers.delete(day.key);
            }
        }
        this.updateDraft({
            cyclingEnabled: enabled,
            dayCalories: shouldInitializeDays ? createDayCalories(current.calories) : current.dayCalories,
            incompleteNumbers,
        });
    }

    protected updateDayCalories({ key, value }: { key: DayCalorieKey; value: number | null }): void {
        const draft = this.withNumberEdit(key, value);
        const dayCalories = value === null ? draft.dayCalories : { ...draft.dayCalories, [key]: value };
        this.replaceDraft({ ...draft, dayCalories });
    }

    protected discard(): void {
        this.draft.set(this.sourceDraft());
        this.dirty.set(false);
    }

    protected async persistAsync(): Promise<boolean> {
        if (this.pendingSave !== null) {
            return this.pendingSave;
        }
        const submittedDraft = this.requireDraft();
        if (!isGoalsDraftValid(submittedDraft)) {
            return false;
        }
        this.pendingSave = this.saveRequest()(buildDraftRequest(submittedDraft))
            .then(saved => {
                if (saved && this.draft() === submittedDraft) {
                    this.discard();
                }
                return saved && !this.dirty();
            })
            .finally(() => {
                this.pendingSave = null;
            });
        return this.pendingSave;
    }

    private updateDraft(change: Partial<GoalsDraft>): void {
        this.draft.update(current => ({ ...(current ?? this.sourceDraft()), ...change }));
        this.dirty.set(true);
    }

    private replaceDraft(draft: GoalsDraft): void {
        this.draft.set(draft);
        this.dirty.set(true);
    }

    private withNumberEdit(key: GoalNumberKey, value: number | null): GoalsDraft {
        const draft = this.requireDraft();
        const incompleteNumbers = new Set(draft.incompleteNumbers);
        if (value === null) {
            incompleteNumbers.add(key);
        } else {
            incompleteNumbers.delete(key);
        }
        return { ...draft, incompleteNumbers };
    }

    private requireDraft(): GoalsDraft {
        return this.draft() ?? this.sourceDraft();
    }

    private sourceDraft(): GoalsDraft {
        const macros: Record<MacroKey, number> = {
            protein: 0,
            fats: 0,
            carbs: 0,
            fiber: 0,
        };
        for (const macro of this.macros()) {
            macros[macro.key] = macro.value;
        }

        return {
            calories: this.calories(),
            macros,
            preset: this.preset(),
            water: this.water(),
            incompleteNumbers: new Set(),
            bodyTargets: { ...this.bodyTargets() },
            invalidBodyTargets: { weight: false, waist: false },
            cyclingEnabled: this.cyclingEnabled(),
            dayCalories: { ...this.dayCalories() },
        };
    }
}

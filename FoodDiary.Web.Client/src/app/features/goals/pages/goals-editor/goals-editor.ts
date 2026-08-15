import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { UnsavedChangesBarComponent } from '../../../../components/shared/unsaved-changes-bar/unsaved-changes-bar';
import { type UnsavedChangesHandler, UnsavedChangesService } from '../../../../services/unsaved-changes.service';
import type { BodyTargetKey, MacroKey, MacroPreset, MacroPresetKey } from '../../lib/goals.facade';
import type { DayCalorieKey, UpdateGoalsRequest } from '../../models/goals.data';
import { GoalsCyclingRowComponent } from './goals-cycling-row';
import { applyMacroPreset, buildDraftRequest, calculateMacroPercent, type GoalsDraft, type GoalsMacroDraft } from './goals-editor.models';
import { GoalsNutritionCardComponent } from './goals-nutrition-card';
import { GoalsSideCardsComponent } from './goals-side-cards';
import { GoalsSummaryCardComponent } from './goals-summary-card';

@Component({
    selector: 'fd-goals-editor',
    imports: [
        TranslatePipe,
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
    public readonly save = output<UpdateGoalsRequest>();

    protected readonly draft = signal<GoalsDraft | null>(null);
    protected readonly dirty = signal(false);
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
            save: () => {
                this.persist();
                return true;
            },
            discard: () => {
                this.discard();
            },
        };
        this.unsavedChangesService.register(handler);
        this.destroyRef.onDestroy(() => {
            this.unsavedChangesService.clear(handler);
        });
    }

    protected updateCalories(value: number): void {
        const current = { ...this.requireDraft(), calories: value };
        const preset = this.presets().find(item => item.key === current.preset);
        this.replaceDraft(preset === undefined ? current : applyMacroPreset(current, preset));
    }

    protected updateMacro(change: { key: MacroKey; value: number }): void {
        const current = this.requireDraft();
        this.updateDraft({ macros: { ...current.macros, [change.key]: change.value }, preset: 'custom' });
    }

    protected updatePreset(value: MacroPresetKey): void {
        const preset = this.presets().find(item => item.key === value);
        this.replaceDraft(preset === undefined ? { ...this.requireDraft(), preset: value } : applyMacroPreset(this.requireDraft(), preset));
    }

    protected updateWater(value: number): void {
        this.updateDraft({ water: value });
    }

    protected updateBodyTarget(change: { key: BodyTargetKey; value: number }): void {
        const current = this.requireDraft();
        this.updateDraft({ bodyTargets: { ...current.bodyTargets, [change.key]: change.value } });
    }

    protected updateCycling(enabled: boolean): void {
        this.updateDraft({ cyclingEnabled: enabled });
    }

    protected updateDayCalories(change: { key: DayCalorieKey; value: number }): void {
        const current = this.requireDraft();
        this.updateDraft({ dayCalories: { ...current.dayCalories, [change.key]: change.value } });
    }

    protected discard(): void {
        this.draft.set(this.sourceDraft());
        this.dirty.set(false);
    }

    protected persist(): void {
        this.save.emit(buildDraftRequest(this.requireDraft()));
        this.dirty.set(false);
    }

    private updateDraft(change: Partial<GoalsDraft>): void {
        this.draft.update(current => ({ ...(current ?? this.sourceDraft()), ...change }));
        this.dirty.set(true);
    }

    private replaceDraft(draft: GoalsDraft): void {
        this.draft.set(draft);
        this.dirty.set(true);
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
            bodyTargets: { ...this.bodyTargets() },
            cyclingEnabled: this.cyclingEnabled(),
            dayCalories: { ...this.dayCalories() },
        };
    }
}

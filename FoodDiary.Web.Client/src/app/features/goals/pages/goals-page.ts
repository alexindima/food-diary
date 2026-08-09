import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { ErrorStateComponent } from '../../../components/shared/error-state/error-state';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header';
import { SkeletonCardComponent } from '../../../components/shared/skeleton-card/skeleton-card';
import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { LocalizedTourDefinitionService } from '../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../shared/ui/layout/page-container.directive';
import { GoalsFacade, type MacroKey, type MacroPresetKey } from '../lib/goals.facade';
import type { UpdateGoalsRequest } from '../models/goals.data';
import { GoalsEditorComponent } from './goals-editor/goals-editor';
import { GOALS_TOUR } from './goals-page-lib/goals-tour';

@Component({
    selector: 'fd-goals-page',
    providers: [GoalsFacade],
    imports: [
        TranslatePipe,
        FdUiHintDirective,
        FdUiButtonComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ErrorStateComponent,
        SkeletonCardComponent,
        GoalsEditorComponent,
    ],
    templateUrl: './goals-page.html',
    styleUrls: ['./goals-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsPageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly facade = inject(GoalsFacade);

    protected readonly calorieTarget = this.facade.calorieTarget;
    protected readonly isLoadingGoals = this.facade.isLoadingGoals;
    protected readonly isSavingGoals = this.facade.isSavingGoals;
    protected readonly hasLoadError = this.facade.hasLoadError;
    protected readonly saveStatusKey = this.facade.saveStatusKey;
    protected readonly macroPresets = this.facade.macroPresets;
    protected readonly selectedPreset = this.facade.selectedPreset;
    protected readonly waterState = this.facade.waterState;
    protected readonly calorieCyclingEnabled = this.facade.calorieCyclingEnabled;
    protected readonly dayCalories = this.facade.dayCalories;
    protected readonly bodyTargetDraftValues = this.facade.bodyTargetValues;
    protected macroPresetOptions: Array<FdUiSelectOption<MacroPresetKey>> = [];

    public constructor() {
        this.buildMacroPresetOptions();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildMacroPresetOptions();
        });
        this.facade.initialize();
    }

    protected reload(): void {
        this.facade.reload();
    }

    protected startGoalsTour(force = true): void {
        this.tourService.start(this.localizedTour.build(GOALS_TOUR), { force });
    }

    protected readonly allMacroViewStates = computed(() =>
        this.facade.macroStates().map(macro => {
            const calorieFactors: Record<MacroKey, number> = { protein: 4, fats: 9, carbs: 4, fiber: 2 };
            const accents: Record<MacroKey, string> = {
                protein: 'var(--fd-color-primary-500)',
                fats: 'var(--fd-color-orange-500)',
                carbs: 'var(--fd-color-sky-500)',
                fiber: 'var(--fd-color-rose-500)',
            };
            const icons: Record<MacroKey, string> = {
                protein: 'fitness_center',
                fats: 'water_drop',
                carbs: 'grass',
                fiber: 'spa',
            };
            const calorieTarget = this.calorieTarget();
            const percent =
                calorieTarget > 0 ? Math.round((macro.value * calorieFactors[macro.key] * PERCENT_MULTIPLIER) / calorieTarget) : 0;

            return { ...macro, accent: accents[macro.key], icon: icons[macro.key], percent };
        }),
    );

    protected saveGoalsManually(request: UpdateGoalsRequest): void {
        this.facade.saveManually(request);
    }

    private buildMacroPresetOptions(): void {
        this.macroPresetOptions = this.macroPresets.map(preset => ({
            value: preset.key,
            label: this.translateService.instant(preset.labelKey),
        }));
    }
}

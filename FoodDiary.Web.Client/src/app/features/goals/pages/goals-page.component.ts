import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';

import { ErrorStateComponent } from '../../../components/shared/error-state/error-state.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { SkeletonCardComponent } from '../../../components/shared/skeleton-card/skeleton-card.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { MAX_BODY_TARGET } from '../lib/goals.constants';
import { type BodyTargetKey, GoalsFacade, type MacroKey, type MacroPresetKey } from '../lib/goals.facade';
import { type DayCalorieKey, DAYS_OF_WEEK } from '../models/goals.data';
import type { BodyTargetInputChange, DayCaloriesInputChange, MacroInputChange } from './goals-page-lib/goals-page.models';
import {
    buildBodyTargets,
    buildCyclingDayControls,
    buildMacroPresetOptions,
    calculateCaloriesFromRingPointer,
    calculateGoalRingDistances,
    isPointInsideGoalRing,
    withMacroProgressStyles,
} from './goals-page-lib/goals-page-view.mapper';
import { GoalsBodyTargetsComponent } from './goals-page-sections/body-targets/goals-body-targets.component';
import { GoalsCalorieCardComponent } from './goals-page-sections/calorie-card/goals-calorie-card.component';
import { GoalsCyclingCardComponent } from './goals-page-sections/cycling-card/goals-cycling-card.component';
import { GoalsFiberCardComponent } from './goals-page-sections/fiber-card/goals-fiber-card.component';
import { GoalsMacrosCardComponent } from './goals-page-sections/macros-card/goals-macros-card.component';
import { GoalsWaterCardComponent } from './goals-page-sections/water-card/goals-water-card.component';

@Component({
    selector: 'fd-goals-page',
    standalone: true,
    providers: [GoalsFacade],
    imports: [
        TranslateModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ErrorStateComponent,
        SkeletonCardComponent,
        GoalsCalorieCardComponent,
        GoalsCyclingCardComponent,
        GoalsWaterCardComponent,
        GoalsMacrosCardComponent,
        GoalsFiberCardComponent,
        GoalsBodyTargetsComponent,
    ],
    templateUrl: './goals-page.component.html',
    styleUrls: ['./goals-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsPageComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly facade = inject(GoalsFacade);

    protected readonly minCalories = this.facade.minCalories;
    protected readonly maxCalories = this.facade.maxCalories;
    protected readonly calorieTarget = this.facade.calorieTarget;
    protected readonly isLoadingGoals = this.facade.isLoadingGoals;
    protected readonly isSavingGoals = this.facade.isSavingGoals;
    protected readonly hasLoadError = this.facade.hasLoadError;
    protected readonly saveStatusKey = this.facade.saveStatusKey;
    protected readonly macroPresets = this.facade.macroPresets;
    protected readonly selectedPreset = this.facade.selectedPreset;
    protected readonly waterState = this.facade.waterState;
    protected readonly coreMacroStates = this.facade.coreMacroStates;
    protected readonly fiberMacroState = this.facade.fiberMacroState;
    protected readonly progressPercent = this.facade.progressPercent;
    protected readonly knobAngle = this.facade.knobAngle;
    protected readonly accentColor = this.facade.accentColor;
    protected readonly calorieCyclingEnabled = this.facade.calorieCyclingEnabled;
    protected readonly dayCalories = this.facade.dayCalories;
    protected readonly daysOfWeek = DAYS_OF_WEEK;
    protected macroPresetOptions: Array<FdUiSelectOption<MacroPresetKey>> = [];
    private activeRingElement: HTMLElement | null = null;

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

    protected readonly currentBodyTargets = computed(() => buildBodyTargets(this.facade.bodyTargetValues()));
    protected readonly ringProgressOffset = computed(() => `${this.progressPercent()}%`);
    protected readonly ringKnobAngle = computed(() => `${this.knobAngle()}deg`);
    protected readonly cyclingDayControls = computed(() => buildCyclingDayControls(this.daysOfWeek));
    protected readonly waterViewState = computed(() => withMacroProgressStyles(this.waterState()));
    protected readonly coreMacroViewStates = computed(() => this.coreMacroStates().map(macro => withMacroProgressStyles(macro)));
    protected readonly fiberMacroViewState = computed(() => {
        const fiber = this.fiberMacroState();
        if (fiber === undefined) {
            return null;
        }

        return withMacroProgressStyles(fiber);
    });

    protected toggleCalorieCycling(): void {
        this.facade.toggleCalorieCycling();
    }

    protected onDayCaloriesInput(key: DayCalorieKey, event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        this.facade.updateDayCalories(key, Number(target.value));
    }

    protected onDayCaloriesInputChange(change: DayCaloriesInputChange): void {
        this.onDayCaloriesInput(change.key, change.event);
    }

    protected onCaloriesInput(event: Event): void {
        this.updateCaloriesFromEvent(event);
    }

    protected onBodyTargetChange(key: BodyTargetKey, event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        const clamped = this.facade.updateBodyTarget(key, Number(target.value), MAX_BODY_TARGET);
        if (clamped !== null) {
            target.value = clamped.toString();
        }
    }

    protected onBodyTargetInput(change: BodyTargetInputChange): void {
        this.onBodyTargetChange(change.key, change.event);
    }

    protected onCaloriesBlur(event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        const clamped = this.facade.normalizeCaloriesInput(Number(target.value));
        target.value = clamped.toString();
    }

    protected onSliderInput(event: Event): void {
        this.updateCaloriesFromEvent(event);
    }

    private updateCaloriesFromEvent(event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        this.facade.updateCalories(Number(target.value));
    }

    protected onMacroPresetModelChange(value: MacroPresetKey | null): void {
        if (value === null) {
            return;
        }

        this.facade.changeMacroPreset(value);
    }

    protected onMacroSliderChange(key: MacroKey, event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        this.facade.updateMacroValue(key, Number(target.value));
    }

    protected onMacroSliderInput(change: MacroInputChange): void {
        this.onMacroSliderChange(change.key, change.event);
    }

    protected onMacroInputChange(key: MacroKey, event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        const clamped = this.facade.updateMacroValue(key, Number(target.value));
        if (clamped !== null) {
            target.value = clamped.toString();
        }
    }

    protected onMacroValueInput(change: MacroInputChange): void {
        this.onMacroInputChange(change.key, change.event);
    }

    protected onWaterInputChange(event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        const clamped = this.facade.updateWaterValue(Number(target.value));
        if (clamped !== null) {
            target.value = clamped.toString();
        }
    }

    protected onWaterSliderChange(event: Event): void {
        const target = this.getInputTarget(event);
        if (target === null) {
            return;
        }

        this.facade.updateWaterValue(Number(target.value));
    }

    protected onRingHover(event: PointerEvent): void {
        const ring = this.getCurrentElementTarget(event);
        if (ring === null) {
            return;
        }

        ring.style.cursor = isPointInsideGoalRing(event, ring.getBoundingClientRect()) ? 'grab' : 'default';
    }

    protected onRingLeave(event: PointerEvent): void {
        this.getCurrentElementTarget(event)?.style.setProperty('cursor', 'default');
    }

    protected startRingDrag(event: PointerEvent): void {
        const target = event.target instanceof HTMLElement ? event.target : null;
        const possibleRing = this.getCurrentElementTarget(event)?.closest('.goals__ring');
        if (!(possibleRing instanceof HTMLElement)) {
            return;
        }
        const ring = possibleRing;

        const { distanceFromCenter, innerRadius } = calculateGoalRingDistances(event, ring.getBoundingClientRect());

        if (distanceFromCenter < innerRadius || target?.closest('.goals__ring-center') !== null) {
            return;
        }

        event.preventDefault();
        this.activeRingElement = ring;
        this.updateFromPointer(event, ring);
        window.addEventListener('pointermove', this.handlePointerMove);
        window.addEventListener('pointerup', this.stopRingDrag, { once: true });
    }

    private readonly handlePointerMove = (event: PointerEvent): void => {
        if (this.activeRingElement === null) {
            return;
        }
        this.updateFromPointer(event, this.activeRingElement);
    };

    private readonly stopRingDrag = (): void => {
        window.removeEventListener('pointermove', this.handlePointerMove);
        this.activeRingElement = null;
    };

    private updateFromPointer(event: PointerEvent, ring: HTMLElement): void {
        this.facade.updateCalories(
            calculateCaloriesFromRingPointer(event, ring.getBoundingClientRect(), this.minCalories, this.maxCalories),
        );
    }

    private buildMacroPresetOptions(): void {
        this.macroPresetOptions = buildMacroPresetOptions(this.macroPresets, key => this.translateService.instant(key));
    }

    private getInputTarget(event: Event): HTMLInputElement | null {
        return event.target instanceof HTMLInputElement ? event.target : null;
    }

    private getCurrentElementTarget(event: Event): HTMLElement | null {
        return event.currentTarget instanceof HTMLElement ? event.currentTarget : null;
    }
}

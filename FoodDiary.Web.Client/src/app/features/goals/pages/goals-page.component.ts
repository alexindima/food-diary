import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { BodyTargetKey, GoalsFacade, MacroKey, MacroPresetKey } from '../lib/goals.facade';
import { DAYS_OF_WEEK } from '../models/goals.data';

type BodyTarget = {
    key: BodyTargetKey;
    titleKey: string;
    value: number;
    unit: string;
    current?: string | null;
    delta?: string | null;
};

type TimeframeOption = {
    value: 'weekly' | 'monthly' | 'yearly';
    labelKey: string;
};

@Component({
    selector: 'fd-goals-page',
    standalone: true,
    providers: [GoalsFacade],
    imports: [
        CommonModule,
        FormsModule,
        TranslateModule,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiSelectComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
    ],
    templateUrl: './goals-page.component.html',
    styleUrls: ['./goals-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsPageComponent implements OnInit {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly facade = inject(GoalsFacade);

    protected readonly minCalories = this.facade.minCalories;
    protected readonly maxCalories = this.facade.maxCalories;
    protected readonly calorieTarget = this.facade.calorieTarget;
    protected readonly isLoadingGoals = this.facade.isLoadingGoals;
    protected readonly isSavingGoals = this.facade.isSavingGoals;
    protected readonly macroPresets = this.facade.macroPresets;
    protected readonly selectedPreset = this.facade.selectedPreset;
    protected readonly macroStates = this.facade.macroStates;
    protected readonly waterState = this.facade.waterState;
    protected readonly coreMacroStates = this.facade.coreMacroStates;
    protected readonly fiberMacroState = this.facade.fiberMacroState;
    protected readonly progressPercent = this.facade.progressPercent;
    protected readonly knobAngle = this.facade.knobAngle;
    protected readonly accentColor = this.facade.accentColor;
    protected readonly calorieCyclingEnabled = this.facade.calorieCyclingEnabled;
    protected readonly dayCalories = this.facade.dayCalories;
    protected readonly daysOfWeek = DAYS_OF_WEEK;
    protected macroPresetOptions: FdUiSelectOption<MacroPresetKey>[] = [];
    private activeRingElement: HTMLElement | null = null;

    public ngOnInit(): void {
        this.buildMacroPresetOptions();
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildMacroPresetOptions();
        });
        this.facade.initialize();
    }

    protected readonly currentBodyTargets = computed(() =>
        this.bodyTargets.map(target => ({
            ...target,
            value: this.facade.bodyTargetValues()[target.key],
        })),
    );

    protected toggleCalorieCycling(): void {
        this.facade.toggleCalorieCycling();
    }

    protected onDayCaloriesInput(key: string, event: Event): void {
        const target = event.target as HTMLInputElement;
        this.facade.updateDayCalories(key, Number(target.value));
    }

    protected onCaloriesInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.facade.updateCalories(Number(target.value));
    }

    protected onBodyTargetChange(key: BodyTargetKey, event: Event): void {
        const target = event.target as HTMLInputElement;
        const clamped = this.facade.updateBodyTarget(key, Number(target.value), 400);
        if (clamped !== null) {
            target.value = clamped.toString();
        }
    }

    protected onCaloriesBlur(event: Event): void {
        const target = event.target as HTMLInputElement;
        const clamped = this.facade.normalizeCaloriesInput(Number(target.value));
        target.value = clamped.toString();
    }

    protected onSliderInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.facade.updateCalories(Number(target.value));
    }

    protected onMacroPresetChange(event: Event): void {
        const target = event.target as HTMLSelectElement;
        this.facade.changeMacroPreset(target.value as MacroPresetKey);
    }

    protected onMacroPresetModelChange(value: MacroPresetKey | null): void {
        if (!value) {
            return;
        }

        this.facade.changeMacroPreset(value);
    }

    protected onMacroSliderChange(key: MacroKey, event: Event): void {
        const target = event.target as HTMLInputElement;
        this.facade.updateMacroValue(key, Number(target.value));
    }

    protected saveGoals(): void {
        this.facade.saveGoals();
    }

    protected onMacroInputChange(key: MacroKey, event: Event): void {
        const target = event.target as HTMLInputElement;
        const clamped = this.facade.updateMacroValue(key, Number(target.value));
        if (clamped !== null) {
            target.value = clamped.toString();
        }
    }

    protected onWaterInputChange(event: Event): void {
        const target = event.target as HTMLInputElement;
        const clamped = this.facade.updateWaterValue(Number(target.value));
        if (clamped !== null) {
            target.value = clamped.toString();
        }
    }

    protected onWaterSliderChange(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.facade.updateWaterValue(Number(target.value));
    }

    protected onRingHover(event: PointerEvent): void {
        const ring = event.currentTarget as HTMLElement;
        const { distanceFromCenter, innerRadius, outerRadius } = this.calculateRingDistances(event, ring);
        const isInBand = distanceFromCenter >= innerRadius && distanceFromCenter <= outerRadius;
        ring.style.cursor = isInBand ? 'grab' : 'default';
    }

    protected onRingLeave(event: PointerEvent): void {
        const ring = event.currentTarget as HTMLElement;
        ring.style.cursor = 'default';
    }

    protected startRingDrag(event: PointerEvent): void {
        const target = event.target as HTMLElement | null;
        const possibleRing = (event.currentTarget as HTMLElement | null)?.closest('.goals__ring');
        if (!(possibleRing instanceof HTMLElement)) {
            return;
        }
        const ring = possibleRing;

        const { distanceFromCenter, innerRadius } = this.calculateRingDistances(event, ring);

        if (distanceFromCenter < innerRadius || target?.closest('.goals__ring-center')) {
            return;
        }

        event.preventDefault();
        this.activeRingElement = ring;
        this.updateFromPointer(event, ring);
        window.addEventListener('pointermove', this.handlePointerMove);
        window.addEventListener('pointerup', this.stopRingDrag, { once: true });
    }

    private readonly handlePointerMove = (event: PointerEvent): void => {
        if (!this.activeRingElement) {
            return;
        }
        this.updateFromPointer(event, this.activeRingElement);
    };

    private readonly stopRingDrag = (): void => {
        window.removeEventListener('pointermove', this.handlePointerMove);
        this.activeRingElement = null;
    };

    private updateFromPointer(event: PointerEvent, ring: HTMLElement): void {
        const { centerX, centerY } = this.calculateRingDistances(event, ring);
        const dx = event.clientX - centerX;
        const dy = event.clientY - centerY;
        const radians = Math.atan2(dy, dx);
        const degrees = (radians * 180) / Math.PI;
        const normalized = (degrees + 450) % 360;
        const ratio = normalized / 360;
        const value = this.minCalories + ratio * (this.maxCalories - this.minCalories);
        this.facade.updateCalories(Math.round(value));
    }

    private calculateRingDistances(
        event: PointerEvent,
        ring: HTMLElement,
    ): {
        rect: DOMRect;
        centerX: number;
        centerY: number;
        distanceFromCenter: number;
        innerRadius: number;
        outerRadius: number;
    } {
        const rect = ring.getBoundingClientRect();
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;
        const distanceFromCenter = Math.hypot(event.clientX - centerX, event.clientY - centerY);
        const outerRadius = Math.min(rect.width, rect.height) / 2;
        const innerRadius = outerRadius - 30;
        return { rect, centerX, centerY, distanceFromCenter, innerRadius, outerRadius };
    }

    private buildMacroPresetOptions(): void {
        this.macroPresetOptions = this.macroPresets.map(preset => ({
            value: preset.key,
            label: this.translateService.instant(preset.labelKey),
        }));
    }

    protected readonly bodyTargets: BodyTarget[] = [
        {
            key: 'weight',
            titleKey: 'GOALS_PAGE.BODY_TARGET_WEIGHT',
            value: 0,
            unit: 'kg',
            current: null,
            delta: null,
        },
        {
            key: 'waist',
            titleKey: 'GOALS_PAGE.BODY_TARGET_WAIST',
            value: 0,
            unit: 'cm',
            current: null,
            delta: null,
        },
    ];

    protected readonly timeframeOptions: TimeframeOption[] = [
        { value: 'weekly', labelKey: 'GOALS_PAGE.TIMEFRAMES.WEEKLY' },
        { value: 'monthly', labelKey: 'GOALS_PAGE.TIMEFRAMES.MONTHLY' },
        { value: 'yearly', labelKey: 'GOALS_PAGE.TIMEFRAMES.YEARLY' },
    ];

    protected readonly activeTimeframe: TimeframeOption['value'] = 'monthly';
}

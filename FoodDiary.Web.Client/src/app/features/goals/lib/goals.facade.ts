import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom, tap } from 'rxjs';

import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import type { DayCalorieKey, GoalsResponse, UpdateGoalsRequest } from '../../../shared/models/goals.data';
import { NutritionDataInvalidationService } from '../../../shared/state/nutrition-data-invalidation.service';
import { GoalsService } from '../api/goals.service';
import { GOALS_DEFAULT_ZONE_ALPHA, GOALS_MAX_CALORIES } from './goals.constants';
import { type BodyTargetKey, clampGoalValue, createDayCalories, createDayCaloriesFromGoals, type MacroKey } from './goals-state.mapper';

export type { BodyTargetKey, MacroKey } from './goals-state.mapper';

type MacroZone = {
    from: number;
    to: number;
    color: string;
};

type MacroZones = [blue: MacroZone, green: MacroZone, orange: MacroZone, red: MacroZone];

type MacroConfig = {
    key: MacroKey;
    labelKey: string;
    unit: string;
    max: number;
    blueTo: number;
    greenFrom: number;
    greenTo: number;
    orangeTo: number;
};

type MacroItem = Omit<MacroConfig, 'blueTo' | 'orangeTo'> & { zones: MacroZones };

export type MacroPresetKey = 'custom' | 'balancedGym' | 'classic' | 'lowCarb' | 'highCarb' | 'endurance';

export type MacroPercent = {
    protein: number;
    fats: number;
    carbs: number;
};

export type MacroPreset = {
    key: MacroPresetKey;
    labelKey: string;
    percent?: MacroPercent;
};

export type MacroPresetCatalog = {
    readonly [Key in MacroPresetKey]: MacroPreset & { key: Key };
};

export const MACRO_PRESETS: MacroPresetCatalog = {
    custom: { key: 'custom', labelKey: 'GOALS_PAGE.MACRO_PRESET_CUSTOM' },
    balancedGym: {
        key: 'balancedGym',
        labelKey: 'GOALS_PAGE.MACRO_PRESET_BALANCED_GYM',
        percent: { protein: 0.4, fats: 0.4, carbs: 0.2 },
    },
    classic: {
        key: 'classic',
        labelKey: 'GOALS_PAGE.MACRO_PRESET_CLASSIC',
        percent: { protein: 0.3, fats: 0.3, carbs: 0.4 },
    },
    lowCarb: {
        key: 'lowCarb',
        labelKey: 'GOALS_PAGE.MACRO_PRESET_LOW_CARB',
        percent: { protein: 0.5, fats: 0.3, carbs: 0.2 },
    },
    highCarb: {
        key: 'highCarb',
        labelKey: 'GOALS_PAGE.MACRO_PRESET_HIGH_CARB',
        percent: { protein: 0.25, fats: 0.25, carbs: 0.5 },
    },
    endurance: {
        key: 'endurance',
        labelKey: 'GOALS_PAGE.MACRO_PRESET_ENDURANCE',
        percent: { protein: 0.2, fats: 0.3, carbs: 0.5 },
    },
};

@Injectable()
export class GoalsFacade {
    private readonly goalsService = inject(GoalsService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly toastService = inject(FdUiToastService);
    private readonly invalidation = inject(NutritionDataInvalidationService);
    private readonly translateService = inject(TranslateService);

    private readonly colorBlue = 'var(--fd-color-primary-600)';
    private readonly colorGreen = 'var(--fd-color-green-500)';
    private readonly colorOrange = 'var(--fd-color-amber-500)';
    private readonly colorRed = 'var(--fd-color-danger)';

    private readonly macroConfigs = (
        [
            {
                key: 'protein',
                labelKey: 'GOALS_PAGE.MACROS.PROTEIN',
                unit: 'g',
                max: 220,
                blueTo: 60,
                greenFrom: 80,
                greenTo: 130,
                orangeTo: 180,
            },
            {
                key: 'fats',
                labelKey: 'GOALS_PAGE.MACROS.FATS',
                unit: 'g',
                max: 160,
                blueTo: 45,
                greenFrom: 55,
                greenTo: 90,
                orangeTo: 130,
            },
            {
                key: 'carbs',
                labelKey: 'GOALS_PAGE.MACROS.CARBS',
                unit: 'g',
                max: 480,
                blueTo: 150,
                greenFrom: 200,
                greenTo: 300,
                orangeTo: 400,
            },
            {
                key: 'fiber',
                labelKey: 'GOALS_PAGE.MACROS.FIBER',
                unit: 'g',
                max: 80,
                blueTo: 15,
                greenFrom: 25,
                greenTo: 40,
                orangeTo: 55,
            },
        ] satisfies MacroConfig[]
    ).map(({ blueTo, orangeTo, ...config }): MacroItem => ({
        ...config,
        zones: [
            { from: 0, to: blueTo, color: this.colorBlue },
            { from: blueTo, to: config.greenTo, color: this.colorGreen },
            { from: config.greenTo, to: orangeTo, color: this.colorOrange },
            { from: orangeTo, to: config.max, color: this.colorRed },
        ],
    }));

    public readonly calorieTarget = signal(0);
    public readonly calorieCyclingEnabled = signal(false);
    public readonly dayCalories = signal<Record<DayCalorieKey, number>>(createDayCalories(0));
    public readonly macroValues = signal<Record<MacroKey, number>>({
        protein: 0,
        fats: 0,
        carbs: 0,
        fiber: 0,
    });
    public readonly waterValue = signal(0);
    public readonly bodyTargetValues = signal<Record<BodyTargetKey, number>>({
        weight: 0,
        waist: 0,
    });
    private readonly goalsRequest = signal<number | undefined>(undefined);
    private readonly goalsResource = rxResource({
        params: this.goalsRequest,
        stream: ({ params }) =>
            this.goalsService.getGoalsStrict().pipe(
                tap(goals => {
                    if (this.goalsRequest() === params) {
                        this.applyLoadedGoals(goals);
                    }
                }),
            ),
    });
    public readonly isLoadingGoals = computed(() => this.goalsRequest() === undefined || this.goalsResource.isLoading());
    public readonly isSavingGoals = signal(false);
    public readonly hasLoadError = computed(() => this.goalsResource.error() !== undefined);
    public readonly macroPresets = MACRO_PRESETS;

    public readonly macroStates = computed(() =>
        this.macroConfigs.map(cfg => {
            const rawValue = this.macroValues()[cfg.key];
            const value = this.clampValue(rawValue, cfg.max);
            const percent = Math.min(PERCENT_MULTIPLIER, Math.max(0, Math.round((value / cfg.max) * PERCENT_MULTIPLIER)));
            const accent = this.pickZoneColor(cfg.zones, value);
            const gradient = this.buildZoneGradient(cfg);
            const shortfall = Math.max(0, Math.ceil(cfg.greenFrom - value));
            const inRange = value >= cfg.greenFrom && value <= cfg.greenTo;

            return { ...cfg, value, percent, accent, gradient, shortfall, inRange };
        }),
    );

    public readonly saveStatusKey = computed(() => {
        if (this.isLoadingGoals()) {
            return 'GOALS_PAGE.STATUS_LOADING';
        }
        return this.isSavingGoals() ? 'GOALS_PAGE.STATUS_SAVING' : null;
    });

    public initialize(): void {
        this.loadGoals();
    }

    public reload(): void {
        if (!this.isLoadingGoals()) {
            this.loadGoals();
        }
    }

    public async saveManuallyAsync(request: UpdateGoalsRequest): Promise<boolean> {
        this.isSavingGoals.set(true);
        try {
            const goals = await firstValueFrom(this.goalsService.updateGoals(request));
            if (goals === null) {
                return this.reportSaveFailure();
            }

            this.applyLoadedGoals(goals);
            this.invalidation.reportGoalMutation();
            if (!this.destroyRef.destroyed) {
                this.toastService.success(this.translateService.instant('GOALS_PAGE.SAVED_TOAST'));
            }
            return true;
        } catch {
            return this.reportSaveFailure();
        } finally {
            this.isSavingGoals.set(false);
        }
    }

    private reportSaveFailure(): false {
        if (!this.destroyRef.destroyed) {
            this.toastService.error(this.translateService.instant('GOALS_PAGE.STATUS_ERROR'));
        }
        return false;
    }

    private loadGoals(): void {
        this.goalsRequest.update(version => (version ?? 0) + 1);
    }

    private applyLoadedGoals(goals: GoalsResponse | null): void {
        this.applyLoadedCalorieTarget(goals);
        this.applyLoadedMacroTargets(goals);
        this.waterValue.set(this.clampOptionalValue(goals?.waterGoal, GOALS_MAX_CALORIES));
        this.bodyTargetValues.set({
            weight: goals?.desiredWeightKg ?? 0,
            waist: goals?.desiredWaistCm ?? 0,
        });
        this.calorieCyclingEnabled.set(goals?.calorieCyclingEnabled ?? false);
        this.dayCalories.set(createDayCaloriesFromGoals(goals));
    }

    private applyLoadedCalorieTarget(goals: GoalsResponse | null): void {
        this.calorieTarget.set(this.clampOptionalValue(goals?.dailyCalorieTarget, GOALS_MAX_CALORIES));
    }

    private applyLoadedMacroTargets(goals: GoalsResponse | null): void {
        const loaded: Record<MacroKey, number | null | undefined> = {
            protein: goals?.proteinTarget,
            fats: goals?.fatTarget,
            carbs: goals?.carbTarget,
            fiber: goals?.fiberTarget,
        };
        const nextMacros = { ...this.macroValues() };
        for (const { key, max } of this.macroConfigs) {
            nextMacros[key] = this.clampOptionalValue(loaded[key], max);
        }
        this.macroValues.set(nextMacros);
    }

    private clampOptionalValue(value: number | null | undefined, max: number): number {
        return value !== undefined && value !== null ? this.clampValue(value, max) : 0;
    }

    private clampValue(value: number, max: number): number {
        return clampGoalValue(value, max);
    }

    private pickZoneColor([blue, green, orange, red]: MacroZones, value: number): string {
        if (value <= blue.to) {
            return blue.color;
        }
        if (value > orange.to) {
            return red.color;
        }
        return value > green.to ? orange.color : green.color;
    }

    private buildZoneGradient(cfg: MacroItem): string {
        const parts = cfg.zones.map(zone => {
            const start = Math.max(0, Math.min(PERCENT_MULTIPLIER, (zone.from / cfg.max) * PERCENT_MULTIPLIER));
            const end = Math.max(0, Math.min(PERCENT_MULTIPLIER, (zone.to / cfg.max) * PERCENT_MULTIPLIER));
            return `${this.withAlpha(zone.color, GOALS_DEFAULT_ZONE_ALPHA)} ${start}% ${end}%`;
        });
        return `linear-gradient(90deg, ${parts.join(', ')})`;
    }

    private withAlpha(color: string, alpha: number): string {
        return `color-mix(in srgb, ${color} ${Math.round(alpha * PERCENT_MULTIPLIER)}%, transparent)`;
    }
}

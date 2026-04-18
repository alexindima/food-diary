import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { finalize } from 'rxjs';
import { GoalsService } from '../api/goals.service';
import { AutosaveQueue, createAutosaveQueue } from '../../../shared/lib/autosave-queue';
import { UpdateGoalsRequest } from '../models/goals.data';

export type MacroKey = 'protein' | 'fats' | 'carbs' | 'fiber';

type MacroZone = {
    from: number;
    to: number;
    color: string;
};

type MacroItem = {
    key: MacroKey;
    labelKey: string;
    unit: string;
    max: number;
    greenFrom: number;
    greenTo: number;
    zones: MacroZone[];
};

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

type SliderConfig = {
    labelKey: string;
    unit: string;
    max: number;
    greenFrom: number;
    greenTo: number;
    zones: MacroZone[];
};

export type BodyTargetKey = 'weight' | 'waist';

@Injectable()
export class GoalsFacade {
    private readonly goalsService = inject(GoalsService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);

    private readonly colorBlue = 'var(--fd-color-primary-600)';
    private readonly colorGreen = 'var(--fd-color-green-500)';
    private readonly colorOrange = 'var(--fd-color-amber-500)';
    private readonly colorRed = 'var(--fd-color-danger)';

    private readonly macroConfigs: MacroItem[] = [
        {
            key: 'protein',
            labelKey: 'GOALS_PAGE.MACROS.PROTEIN',
            unit: 'g',
            max: 220,
            greenFrom: 80,
            greenTo: 130,
            zones: [
                { from: 0, to: 60, color: this.colorBlue },
                { from: 60, to: 130, color: this.colorGreen },
                { from: 130, to: 180, color: this.colorOrange },
                { from: 180, to: 220, color: this.colorRed },
            ],
        },
        {
            key: 'fats',
            labelKey: 'GOALS_PAGE.MACROS.FATS',
            unit: 'g',
            max: 160,
            greenFrom: 55,
            greenTo: 90,
            zones: [
                { from: 0, to: 45, color: this.colorBlue },
                { from: 45, to: 90, color: this.colorGreen },
                { from: 90, to: 130, color: this.colorOrange },
                { from: 130, to: 160, color: this.colorRed },
            ],
        },
        {
            key: 'carbs',
            labelKey: 'GOALS_PAGE.MACROS.CARBS',
            unit: 'g',
            max: 480,
            greenFrom: 200,
            greenTo: 300,
            zones: [
                { from: 0, to: 150, color: this.colorBlue },
                { from: 150, to: 300, color: this.colorGreen },
                { from: 300, to: 400, color: this.colorOrange },
                { from: 400, to: 480, color: this.colorRed },
            ],
        },
        {
            key: 'fiber',
            labelKey: 'GOALS_PAGE.MACROS.FIBER',
            unit: 'g',
            max: 80,
            greenFrom: 25,
            greenTo: 40,
            zones: [
                { from: 0, to: 15, color: this.colorBlue },
                { from: 15, to: 40, color: this.colorGreen },
                { from: 40, to: 55, color: this.colorOrange },
                { from: 55, to: 80, color: this.colorRed },
            ],
        },
    ];

    private readonly waterConfig: SliderConfig = {
        labelKey: 'GOALS_PAGE.WATER_LABEL',
        unit: 'ml',
        max: 5000,
        greenFrom: 1200,
        greenTo: 2500,
        zones: [
            { from: 0, to: 1200, color: this.colorBlue },
            { from: 1200, to: 2500, color: this.colorGreen },
            { from: 2500, to: 3500, color: this.colorOrange },
            { from: 3500, to: 5000, color: this.colorRed },
        ],
    };

    private readonly autosaveQueue: AutosaveQueue<UpdateGoalsRequest> = createAutosaveQueue({
        debounceMs: 700,
        isBusy: () => this.isSavingGoals(),
        persist: request => this.persistGoals(request),
    });

    public readonly minCalories = 0;
    public readonly maxCalories = 5000;
    public readonly calorieTarget = signal(0);
    public readonly calorieCyclingEnabled = signal(false);
    public readonly dayCalories = signal<Record<string, number>>({
        mondayCalories: 0,
        tuesdayCalories: 0,
        wednesdayCalories: 0,
        thursdayCalories: 0,
        fridayCalories: 0,
        saturdayCalories: 0,
        sundayCalories: 0,
    });
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
    public readonly isLoadingGoals = signal(true);
    public readonly isSavingGoals = signal(false);
    public readonly selectedPreset = signal<MacroPresetKey>('custom');
    public readonly hasPendingAutosave = signal(false);
    public readonly hasAutosaveError = signal(false);

    public readonly macroPresets: MacroPreset[] = [
        { key: 'custom', labelKey: 'GOALS_PAGE.MACRO_PRESET_CUSTOM' },
        {
            key: 'balancedGym',
            labelKey: 'GOALS_PAGE.MACRO_PRESET_BALANCED_GYM',
            percent: { protein: 0.4, fats: 0.4, carbs: 0.2 },
        },
        {
            key: 'classic',
            labelKey: 'GOALS_PAGE.MACRO_PRESET_CLASSIC',
            percent: { protein: 0.3, fats: 0.3, carbs: 0.4 },
        },
        {
            key: 'lowCarb',
            labelKey: 'GOALS_PAGE.MACRO_PRESET_LOW_CARB',
            percent: { protein: 0.5, fats: 0.3, carbs: 0.2 },
        },
        {
            key: 'highCarb',
            labelKey: 'GOALS_PAGE.MACRO_PRESET_HIGH_CARB',
            percent: { protein: 0.25, fats: 0.25, carbs: 0.5 },
        },
        {
            key: 'endurance',
            labelKey: 'GOALS_PAGE.MACRO_PRESET_ENDURANCE',
            percent: { protein: 0.2, fats: 0.3, carbs: 0.5 },
        },
    ];

    private readonly presetSync = effect(() => {
        const presetKey = this.selectedPreset();
        if (presetKey === 'custom') {
            return;
        }

        const preset = this.macroPresets.find(item => item.key === presetKey);
        if (!preset?.percent) {
            return;
        }

        this.applyPresetPercent(preset.percent);
    });

    public readonly macroStates = computed(() =>
        this.macroConfigs.map(cfg => {
            const rawValue = this.macroValues()[cfg.key] ?? 0;
            const value = this.clampValue(rawValue, cfg.max);
            const percent = Math.min(100, Math.max(0, Math.round((value / cfg.max) * 100)));
            const accent = this.pickZoneColor(cfg, value);
            const gradient = this.buildZoneGradient(cfg);
            const shortfall = Math.max(0, Math.ceil(cfg.greenFrom - value));
            const inRange = value >= cfg.greenFrom && value <= cfg.greenTo;

            return { ...cfg, value, percent, accent, gradient, shortfall, inRange };
        }),
    );

    public readonly waterState = computed(() => {
        const rawValue = this.waterValue();
        const value = this.clampValue(rawValue, this.waterConfig.max);
        const percent = Math.min(100, Math.max(0, Math.round((value / this.waterConfig.max) * 100)));
        const accent = this.pickZoneColor(this.waterConfig, value);
        const gradient = this.buildZoneGradient(this.waterConfig);
        const shortfall = Math.max(0, Math.ceil(this.waterConfig.greenFrom - value));
        const inRange = value >= this.waterConfig.greenFrom && value <= this.waterConfig.greenTo;

        return { ...this.waterConfig, value, percent, accent, gradient, shortfall, inRange };
    });

    public readonly coreMacroStates = computed(() => this.macroStates().filter(macro => macro.key !== 'fiber'));
    public readonly fiberMacroState = computed(() => this.macroStates().find(macro => macro.key === 'fiber'));
    public readonly progressPercent = computed(() => {
        const span = this.maxCalories - this.minCalories;
        const normalized = Math.min(Math.max(this.calorieTarget() - this.minCalories, 0), span);
        return Math.round((normalized / span) * 100);
    });
    public readonly knobAngle = computed(() => (this.progressPercent() / 100) * 360);
    public readonly accentColor = computed(() => {
        const value = this.calorieTarget();
        if (value < 1000) {
            return this.colorBlue;
        }
        if (value <= 3500) {
            return this.colorGreen;
        }
        if (value <= 4500) {
            return this.colorOrange;
        }
        return this.colorRed;
    });
    public readonly saveStatusKey = computed(() => {
        if (this.isLoadingGoals()) {
            return 'GOALS_PAGE.STATUS_LOADING';
        }
        if (this.isSavingGoals()) {
            return 'GOALS_PAGE.STATUS_SAVING';
        }
        if (this.hasAutosaveError()) {
            return 'GOALS_PAGE.STATUS_ERROR';
        }
        if (this.hasPendingAutosave()) {
            return 'GOALS_PAGE.STATUS_PENDING';
        }
        return null;
    });

    public initialize(): void {
        this.loadGoals();
    }

    public updateCalories(rawValue: number): void {
        if (Number.isNaN(rawValue)) {
            return;
        }

        this.calorieTarget.set(this.clampCalories(rawValue));
        this.reapplyPresetIfNeeded();
        this.queueAutosave();
    }

    public normalizeCaloriesInput(value: number): number {
        const clamped = this.clampCalories(value);
        this.calorieTarget.set(clamped);
        this.reapplyPresetIfNeeded();
        this.queueAutosave();
        return clamped;
    }

    public updateBodyTarget(key: BodyTargetKey, value: number, max = 400): number | null {
        if (Number.isNaN(value)) {
            return null;
        }

        const clamped = this.clampValue(value, max);
        this.bodyTargetValues.update(current => ({
            ...current,
            [key]: clamped,
        }));
        this.queueAutosave();
        return clamped;
    }

    public changeMacroPreset(next: MacroPresetKey): void {
        const preset = this.macroPresets.find(item => item.key === next);
        if (preset?.percent) {
            this.applyPresetPercent(preset.percent);
        }
        this.selectedPreset.set(next);
        this.queueAutosave();
    }

    public updateMacroValue(key: MacroKey, value: number): number | null {
        const cfg = this.macroConfigs.find(item => item.key === key);
        if (!cfg || Number.isNaN(value)) {
            return null;
        }

        const clamped = this.clampValue(value, cfg.max);
        this.macroValues.update(current => ({
            ...current,
            [key]: clamped,
        }));
        this.selectedPreset.set('custom');
        this.queueAutosave();
        return clamped;
    }

    public updateWaterValue(value: number): number | null {
        if (Number.isNaN(value)) {
            return null;
        }

        const clamped = this.clampValue(value, this.waterConfig.max);
        this.waterValue.set(clamped);
        this.queueAutosave();
        return clamped;
    }

    public toggleCalorieCycling(): void {
        const next = !this.calorieCyclingEnabled();
        this.calorieCyclingEnabled.set(next);
        if (next) {
            const base = this.calorieTarget();
            const current = this.dayCalories();
            const allZero = Object.values(current).every(v => v === 0);
            if (allZero && base > 0) {
                this.dayCalories.set({
                    mondayCalories: base,
                    tuesdayCalories: base,
                    wednesdayCalories: base,
                    thursdayCalories: base,
                    fridayCalories: base,
                    saturdayCalories: base,
                    sundayCalories: base,
                });
            }
        }
        this.queueAutosave();
    }

    public updateDayCalories(key: string, value: number): void {
        if (Number.isNaN(value)) {
            return;
        }

        const clamped = this.clampCalories(value);
        this.dayCalories.update(current => ({ ...current, [key]: clamped }));
        this.queueAutosave();
    }

    private loadGoals(): void {
        this.isLoadingGoals.set(true);
        this.goalsService
            .getGoals()
            .pipe(finalize(() => this.isLoadingGoals.set(false)))
            .subscribe(goals => {
                this.selectedPreset.set('custom');

                if (goals?.dailyCalorieTarget !== undefined && goals.dailyCalorieTarget !== null) {
                    this.calorieTarget.set(this.clampCalories(goals.dailyCalorieTarget));
                }

                const nextMacros = { ...this.macroValues() };
                let hasUserMacros = false;

                const applyMacro = (key: MacroKey, value: number | null | undefined): void => {
                    if (value === null || value === undefined) {
                        return;
                    }

                    const cfg = this.macroConfigs.find(item => item.key === key);
                    if (!cfg) {
                        return;
                    }

                    nextMacros[key] = this.clampValue(value, cfg.max);
                    hasUserMacros = true;
                };

                applyMacro('protein', goals?.proteinTarget);
                applyMacro('fats', goals?.fatTarget);
                applyMacro('carbs', goals?.carbTarget);
                applyMacro('fiber', goals?.fiberTarget);

                if (hasUserMacros) {
                    this.macroValues.set(nextMacros);
                }

                this.waterValue.set(
                    goals?.waterGoal !== undefined && goals.waterGoal !== null ? this.clampValue(goals.waterGoal, this.waterConfig.max) : 0,
                );

                this.bodyTargetValues.set({
                    weight: goals?.desiredWeight ?? 0,
                    waist: goals?.desiredWaist ?? 0,
                });

                this.calorieCyclingEnabled.set(goals?.calorieCyclingEnabled ?? false);
                this.dayCalories.set({
                    mondayCalories: goals?.mondayCalories ?? 0,
                    tuesdayCalories: goals?.tuesdayCalories ?? 0,
                    wednesdayCalories: goals?.wednesdayCalories ?? 0,
                    thursdayCalories: goals?.thursdayCalories ?? 0,
                    fridayCalories: goals?.fridayCalories ?? 0,
                    saturdayCalories: goals?.saturdayCalories ?? 0,
                    sundayCalories: goals?.sundayCalories ?? 0,
                });

                this.hasAutosaveError.set(false);
                this.hasPendingAutosave.set(false);
            });
    }

    private queueAutosave(): void {
        this.hasAutosaveError.set(false);
        this.hasPendingAutosave.set(true);
        this.autosaveQueue.schedule(this.buildGoalsRequest());
    }

    private persistGoals(request: UpdateGoalsRequest): void {
        this.hasPendingAutosave.set(false);
        this.isSavingGoals.set(true);
        this.goalsService
            .updateGoals(request)
            .pipe(finalize(() => this.isSavingGoals.set(false)))
            .subscribe({
                next: goals => {
                    if (!goals) {
                        const hasQueuedUpdate = this.autosaveQueue.hasPending();
                        if (!hasQueuedUpdate) {
                            this.autosaveQueue.restore(request);
                        } else {
                            this.autosaveQueue.scheduleIfPending();
                        }

                        this.hasAutosaveError.set(true);
                        this.hasPendingAutosave.set(this.autosaveQueue.hasPending());
                        return;
                    }

                    this.hasAutosaveError.set(false);
                    this.toastService.success(this.translateService.instant('GOALS_PAGE.SAVED_TOAST'));
                    this.autosaveQueue.scheduleIfPending();
                    this.hasPendingAutosave.set(this.autosaveQueue.hasPending());
                },
                error: () => {
                    const hasQueuedUpdate = this.autosaveQueue.hasPending();
                    if (!hasQueuedUpdate) {
                        this.autosaveQueue.restore(request);
                    } else {
                        this.autosaveQueue.scheduleIfPending();
                    }

                    this.hasAutosaveError.set(true);
                    this.hasPendingAutosave.set(this.autosaveQueue.hasPending());
                },
            });
    }

    private buildGoalsRequest(): UpdateGoalsRequest {
        const macros = this.macroValues();
        const bodyTargets = this.bodyTargetValues();

        return {
            dailyCalorieTarget: this.calorieTarget(),
            proteinTarget: macros.protein,
            fatTarget: macros.fats,
            carbTarget: macros.carbs,
            fiberTarget: macros.fiber,
            waterGoal: this.waterValue(),
            desiredWeight: this.normalizeDesiredBodyTarget(bodyTargets.weight),
            desiredWaist: this.normalizeDesiredBodyTarget(bodyTargets.waist),
            calorieCyclingEnabled: this.calorieCyclingEnabled(),
            ...(this.calorieCyclingEnabled() ? this.dayCalories() : {}),
        };
    }

    private normalizeDesiredBodyTarget(value: number): number | null {
        return value > 0 ? value : null;
    }

    private clampCalories(value: number): number {
        return Math.min(this.maxCalories, Math.max(this.minCalories, Math.round(value)));
    }

    private clampValue(value: number, max: number): number {
        return Math.min(max, Math.max(0, Math.round(value)));
    }

    private pickZoneColor(cfg: SliderConfig | MacroItem, value: number): string {
        const zone = cfg.zones.find(item => value >= item.from && value <= item.to);
        return zone?.color ?? this.colorGreen;
    }

    private buildZoneGradient(cfg: SliderConfig | MacroItem): string {
        const parts = cfg.zones.map(zone => {
            const start = Math.max(0, Math.min(100, (zone.from / cfg.max) * 100));
            const end = Math.max(0, Math.min(100, (zone.to / cfg.max) * 100));
            return `${this.withAlpha(zone.color, 0.16)} ${start}% ${end}%`;
        });
        return `linear-gradient(90deg, ${parts.join(', ')})`;
    }

    private withAlpha(color: string, alpha: number): string {
        return `color-mix(in srgb, ${color} ${Math.round(alpha * 100)}%, transparent)`;
    }

    private applyPresetPercent(percent: MacroPercent): void {
        const calories = this.calorieTarget();
        const targetValues: Partial<Record<MacroKey, number>> = {
            protein: (calories * percent.protein) / 4,
            fats: (calories * percent.fats) / 9,
            carbs: (calories * percent.carbs) / 4,
        };

        this.macroValues.update(current => {
            let changed = false;
            const next = { ...current };

            (Object.keys(targetValues) as MacroKey[]).forEach(key => {
                const cfg = this.macroConfigs.find(item => item.key === key);
                const raw = targetValues[key];
                if (!cfg || raw === undefined) {
                    return;
                }

                const clamped = this.clampValue(Math.round(raw), cfg.max);
                if (next[key] !== clamped) {
                    next[key] = clamped;
                    changed = true;
                }
            });

            return changed ? next : current;
        });
    }

    private reapplyPresetIfNeeded(): void {
        const presetKey = this.selectedPreset();
        if (presetKey === 'custom') {
            return;
        }

        const preset = this.macroPresets.find(item => item.key === presetKey);
        if (preset?.percent) {
            this.applyPresetPercent(preset.percent);
        }
    }
}

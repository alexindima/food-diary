import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { GoalsService } from '../../services/goals.service';
import { finalize } from 'rxjs';

type MacroKey = 'protein' | 'fats' | 'carbs' | 'fiber';

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

type MacroPresetKey = 'custom' | 'balancedGym' | 'classic' | 'lowCarb' | 'highCarb' | 'endurance';

type MacroPreset = {
    key: MacroPresetKey;
    labelKey: string;
    percent?: MacroPercent;
};

type MacroPercent = {
    protein: number;
    fats: number;
    carbs: number;
};

type SliderConfig = {
    labelKey: string;
    unit: string;
    max: number;
    greenFrom: number;
    greenTo: number;
    zones: MacroZone[];
};

type BodyTargetKey = 'weight' | 'waist';

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
    imports: [
        CommonModule,
        TranslateModule,
        FdUiButtonComponent,
        FdUiCardComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
    ],
    templateUrl: './goals-page.component.html',
    styleUrls: ['./goals-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoalsPageComponent implements OnInit {
    private readonly goalsService = inject(GoalsService);

    protected readonly minCalories = 0;
    protected readonly maxCalories = 5000;
    protected readonly calorieTarget = signal(0);
    private activeRingElement: HTMLElement | null = null;
    private readonly colorBlue = '#2563eb';
    private readonly colorGreen = '#16a34a';
    private readonly colorOrange = '#f59e0b';
    private readonly colorRed = '#ef4444';

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

    private readonly macroValues = signal<Record<MacroKey, number>>({
        protein: 0,
        fats: 0,
        carbs: 0,
        fiber: 0,
    });
    private readonly waterValue = signal(0);
    private readonly bodyTargetValues = signal<Record<BodyTargetKey, number>>({
        weight: 0,
        waist: 0,
    });
    protected readonly isLoadingGoals = signal(true);
    protected readonly isSavingGoals = signal(false);

    protected readonly macroPresets: MacroPreset[] = [
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

    protected readonly selectedPreset = signal<MacroPresetKey>('custom');
    private readonly presetSync = effect(() => {
        const presetKey = this.selectedPreset();
        if (presetKey === 'custom') {
            return;
        }
        const preset = this.macroPresets.find(p => p.key === presetKey);
        if (!preset?.percent) {
            return;
        }
        this.applyPresetPercent(preset.percent);
    });

    public ngOnInit(): void {
        this.loadGoals();
    }

    private loadGoals(): void {
        this.isLoadingGoals.set(true);
        this.goalsService
            .getGoals()
            .pipe(finalize(() => this.isLoadingGoals.set(false)))
            .subscribe(goals => {
                // prevent preset effect from overriding loaded values
                this.selectedPreset.set('custom');

                if (goals?.dailyCalorieTarget !== undefined && goals?.dailyCalorieTarget !== null) {
                    this.calorieTarget.set(this.clampCalories(goals.dailyCalorieTarget));
                }

                const nextMacros = { ...this.macroValues() };
                let hasUserMacros = false;

                const applyMacro = (key: MacroKey, value: number | null | undefined): void => {
                    if (value === null || value === undefined) {
                        return;
                    }
                    const cfg = this.macroConfigs.find(c => c.key === key);
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

                if (goals?.waterGoal !== undefined && goals?.waterGoal !== null) {
                    this.waterValue.set(this.clampValue(goals.waterGoal, this.waterConfig.max));
                } else {
                    this.waterValue.set(0);
                }

                this.bodyTargetValues.set({
                    weight: goals?.desiredWeight ?? 0,
                    waist: goals?.desiredWaist ?? 0,
                });
            });
    }

    protected readonly macroStates = computed(() =>
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

    protected readonly waterState = computed(() => {
        const cfg = this.waterConfig;
        const rawValue = this.waterValue();
        const value = this.clampValue(rawValue, cfg.max);
        const percent = Math.min(100, Math.max(0, Math.round((value / cfg.max) * 100)));
        const accent = this.pickZoneColor(cfg, value);
        const gradient = this.buildZoneGradient(cfg);
        const shortfall = Math.max(0, Math.ceil(cfg.greenFrom - value));
        const inRange = value >= cfg.greenFrom && value <= cfg.greenTo;

        return { ...cfg, value, percent, accent, gradient, shortfall, inRange };
    });

    protected readonly coreMacroStates = computed(() =>
        this.macroStates().filter(macro => macro.key !== 'fiber'),
    );

    protected readonly fiberMacroState = computed(() => this.macroStates().find(m => m.key === 'fiber'));
    protected readonly currentBodyTargets = computed(() =>
        this.bodyTargets.map(target => ({
            ...target,
            value: this.bodyTargetValues()[target.key],
        })),
    );

    protected readonly progressPercent = computed(() => {
        const span = this.maxCalories - this.minCalories;
        const normalized = Math.min(Math.max(this.calorieTarget() - this.minCalories, 0), span);
        return Math.round((normalized / span) * 100);
    });

    protected readonly knobAngle = computed(() => (this.progressPercent() / 100) * 360);

    protected readonly accentColor = computed(() => {
        const value = this.calorieTarget();
        if (value < 1000) {
            return '#2563eb';
        }
        if (value <= 3500) {
            return '#16a34a';
        }
        if (value <= 4500) {
            return '#f59e0b';
        }
        return '#ef4444';
    });

    protected onCaloriesInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.updateCalories(Number(target.value));
    }

    protected onBodyTargetChange(key: BodyTargetKey, event: Event): void {
        const target = event.target as HTMLInputElement;
        const numeric = Number(target.value);
        if (Number.isNaN(numeric)) {
            return;
        }
        const clamped = this.clampValue(numeric, 400);
        target.value = clamped.toString();
        this.bodyTargetValues.update(current => ({
            ...current,
            [key]: clamped,
        }));
    }

    protected onCaloriesBlur(event: Event): void {
        const target = event.target as HTMLInputElement;
        const clamped = this.clampCalories(Number(target.value));
        target.value = clamped.toString();
        this.calorieTarget.set(clamped);
        this.reapplyPresetIfNeeded();
    }

    protected onSliderInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.updateCalories(Number(target.value));
    }

    protected onMacroPresetChange(event: Event): void {
        const target = event.target as HTMLSelectElement;
        const next = target.value as MacroPresetKey;
        const preset = this.macroPresets.find(p => p.key === next);
        if (preset?.percent) {
            this.applyPresetPercent(preset.percent);
        }
        this.selectedPreset.set(next);
    }

    protected onMacroSliderChange(key: MacroKey, event: Event): void {
        const target = event.target as HTMLInputElement;
        this.updateMacroValue(key, Number(target.value));
        this.selectedPreset.set('custom');
    }

    protected saveGoals(): void {
        if (this.isSavingGoals()) {
            return;
        }

        const macros = this.macroValues();
        const bodyTargets = this.bodyTargetValues();

        this.isSavingGoals.set(true);

        this.goalsService
            .updateGoals({
                dailyCalorieTarget: this.calorieTarget(),
                proteinTarget: macros.protein,
                fatTarget: macros.fats,
                carbTarget: macros.carbs,
                fiberTarget: macros.fiber,
                waterGoal: this.waterValue(),
                desiredWeight: bodyTargets.weight,
                desiredWaist: bodyTargets.waist,
            })
            .pipe(finalize(() => this.isSavingGoals.set(false)))
            .subscribe();
    }

    protected onMacroInputChange(key: MacroKey, event: Event): void {
        const target = event.target as HTMLInputElement;
        const numeric = Number(target.value);
        const cfg = this.macroConfigs.find(c => c.key === key);
        if (!cfg || Number.isNaN(numeric)) {
            return;
        }
        const clamped = this.clampValue(numeric, cfg.max);
        target.value = clamped.toString();
        this.updateMacroValue(key, clamped);
        this.selectedPreset.set('custom');
    }

    protected onWaterInputChange(event: Event): void {
        const target = event.target as HTMLInputElement;
        const numeric = Number(target.value);
        if (Number.isNaN(numeric)) {
            return;
        }
        const clamped = this.clampValue(numeric, this.waterConfig.max);
        target.value = clamped.toString();
        this.waterValue.set(clamped);
    }

    protected onWaterSliderChange(event: Event): void {
        const target = event.target as HTMLInputElement;
        const numeric = Number(target.value);
        if (Number.isNaN(numeric)) {
            return;
        }
        this.waterValue.set(this.clampValue(numeric, this.waterConfig.max));
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
        const normalized = (degrees + 450) % 360; // start from top, clockwise
        const ratio = normalized / 360;
        const value = this.minCalories + ratio * (this.maxCalories - this.minCalories);
        this.updateCalories(Math.round(value));
        this.reapplyPresetIfNeeded();
    }

    private calculateRingDistances(event: PointerEvent, ring: HTMLElement): {
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

    private updateCalories(rawValue: number): void {
        if (Number.isNaN(rawValue)) {
            return;
        }
        this.calorieTarget.set(this.clampCalories(rawValue));
        this.reapplyPresetIfNeeded();
    }

    private clampCalories(value: number): number {
        return Math.min(this.maxCalories, Math.max(this.minCalories, Math.round(value)));
    }

    private clampValue(value: number, max: number): number {
        return Math.min(max, Math.max(0, Math.round(value)));
    }

    private updateMacroValue(key: MacroKey, value: number): void {
        this.macroValues.update(current => ({
            ...current,
            [key]: value,
        }));
    }

    private pickZoneColor(cfg: SliderConfig | MacroItem, value: number): string {
        const zone = cfg.zones.find(z => value >= z.from && value <= z.to);
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
        if (color.startsWith('#')) {
            const r = parseInt(color.slice(1, 3), 16);
            const g = parseInt(color.slice(3, 5), 16);
            const b = parseInt(color.slice(5, 7), 16);
            return `rgba(${r}, ${g}, ${b}, ${alpha})`;
        }
        return color;
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
                const cfg = this.macroConfigs.find(c => c.key === key);
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
        const preset = this.macroPresets.find(p => p.key === presetKey);
        if (preset?.percent) {
            this.applyPresetPercent(preset.percent);
        }
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

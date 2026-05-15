import { computed, type Signal, signal, type Type, type WritableSignal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { GoalsFacade, type MacroPreset } from '../lib/goals.facade';
import type { DayCalorieKey } from '../models/goals.data';
import { GoalsPageComponent } from './goals-page.component';
import { GoalsBodyTargetsComponent } from './goals-page-sections/body-targets/goals-body-targets.component';
import { GoalsCalorieCardComponent } from './goals-page-sections/calorie-card/goals-calorie-card.component';
import { GoalsCyclingCardComponent } from './goals-page-sections/cycling-card/goals-cycling-card.component';
import { GoalsFiberCardComponent } from './goals-page-sections/fiber-card/goals-fiber-card.component';
import { GoalsMacrosCardComponent } from './goals-page-sections/macros-card/goals-macros-card.component';
import { GoalsWaterCardComponent } from './goals-page-sections/water-card/goals-water-card.component';

const CALORIE_TARGET = 2100;
const NORMALIZED_CALORIES = 4000;
const BODY_TARGET = 72;
const CLAMPED_BODY_TARGET = 300;
const PROTEIN_TARGET = 150;
const CLAMPED_PROTEIN_TARGET = 220;
const WATER_TARGET = 2200;
const CLAMPED_WATER_TARGET = 4000;
const RAW_INPUT_VALUE = 9999;
const DAY_CALORIES_INPUT_VALUE = 2300;
const MAX_BODY_TARGET = 400;
const PROGRESS_PERCENT = 53;
const KNOB_ANGLE = 191;

let facade: GoalsFacadeMock;

describe('GoalsPageComponent', () => {
    beforeEach(async () => {
        facade = createFacadeMock();

        await TestBed.configureTestingModule({
            imports: [GoalsPageComponent, TranslateModule.forRoot()],
        })
            .overrideComponent(GoalsPageComponent, {
                set: {
                    providers: [{ provide: GoalsFacade, useValue: facade }],
                },
            })
            .compileComponents();
    });

    it('initializes goals and passes facade state to sections', () => {
        const fixture = createComponent();

        expect(facade.initialize).toHaveBeenCalledTimes(1);
        expect(getChild(fixture, GoalsCalorieCardComponent).calorieTarget()).toBe(CALORIE_TARGET);
        expect(getChild(fixture, GoalsCalorieCardComponent).ringProgressOffset()).toBe(`${PROGRESS_PERCENT}%`);
        expect(getChild(fixture, GoalsCalorieCardComponent).ringKnobAngle()).toBe(`${KNOB_ANGLE}deg`);
        expect(getChild(fixture, GoalsCyclingCardComponent).dayControls()[0]).toEqual(
            expect.objectContaining({ key: 'mondayCalories', inputId: 'cycling-day-mondayCalories' }),
        );
        expect(getChild(fixture, GoalsWaterCardComponent).water().value).toBe(WATER_TARGET);
        expect(getChild(fixture, GoalsMacrosCardComponent).macros()[0].value).toBe(PROTEIN_TARGET);
        expect(getChild(fixture, GoalsFiberCardComponent).fiber()?.key).toBe('fiber');
        expect(getChild(fixture, GoalsBodyTargetsComponent).targets()[0]).toEqual(
            expect.objectContaining({ key: 'weight', value: BODY_TARGET }),
        );
    });

    it('renders load error and delegates retry', () => {
        facade.hasLoadError.set(true);
        const fixture = createComponent();

        fixture.debugElement.query(By.css('fd-error-state')).triggerEventHandler('retry');

        expect(facade.reload).toHaveBeenCalledTimes(1);
    });

    it('delegates section events to facade methods and writes clamped values back to inputs', () => {
        const fixture = createComponent();
        const caloriesInput = createInputEvent(RAW_INPUT_VALUE.toString());
        const bodyInput = createInputEvent(RAW_INPUT_VALUE.toString());
        const macroInput = createInputEvent(RAW_INPUT_VALUE.toString());
        const waterInput = createInputEvent(RAW_INPUT_VALUE.toString());

        getChild(fixture, GoalsCalorieCardComponent).caloriesInput.emit(caloriesInput.event);
        getChild(fixture, GoalsCalorieCardComponent).caloriesBlur.emit(caloriesInput.event);
        getChild(fixture, GoalsCyclingCardComponent).enabledToggle.emit();
        getChild(fixture, GoalsCyclingCardComponent).dayCaloriesInput.emit({
            key: 'mondayCalories',
            event: createInputEvent(DAY_CALORIES_INPUT_VALUE.toString()).event,
        });
        getChild(fixture, GoalsMacrosCardComponent).presetChange.emit('classic');
        getChild(fixture, GoalsMacrosCardComponent).macroInput.emit({ key: 'protein', event: macroInput.event });
        getChild(fixture, GoalsWaterCardComponent).waterInput.emit(waterInput.event);
        getChild(fixture, GoalsBodyTargetsComponent).targetInput.emit({ key: 'weight', event: bodyInput.event });

        expect(facade.updateCalories).toHaveBeenCalledWith(RAW_INPUT_VALUE);
        expect(facade.normalizeCaloriesInput).toHaveBeenCalledWith(RAW_INPUT_VALUE);
        expect(caloriesInput.target.value).toBe(NORMALIZED_CALORIES.toString());
        expect(facade.toggleCalorieCycling).toHaveBeenCalledTimes(1);
        expect(facade.updateDayCalories).toHaveBeenCalledWith('mondayCalories', DAY_CALORIES_INPUT_VALUE);
        expect(facade.changeMacroPreset).toHaveBeenCalledWith('classic');
        expect(facade.updateMacroValue).toHaveBeenCalledWith('protein', RAW_INPUT_VALUE);
        expect(macroInput.target.value).toBe(CLAMPED_PROTEIN_TARGET.toString());
        expect(facade.updateWaterValue).toHaveBeenCalledWith(RAW_INPUT_VALUE);
        expect(waterInput.target.value).toBe(CLAMPED_WATER_TARGET.toString());
        expect(facade.updateBodyTarget).toHaveBeenCalledWith('weight', RAW_INPUT_VALUE, MAX_BODY_TARGET);
        expect(bodyInput.target.value).toBe(CLAMPED_BODY_TARGET.toString());
    });
});

function createComponent(): ComponentFixture<GoalsPageComponent> {
    const fixture = TestBed.createComponent(GoalsPageComponent);
    fixture.detectChanges();

    return fixture;
}

function getChild<T>(fixture: ComponentFixture<GoalsPageComponent>, type: Type<T>): T {
    const child = fixture.debugElement.query(By.directive(type));
    return child.componentInstance as T;
}

function createInputEvent(value: string): { event: Event; target: HTMLInputElement } {
    const target = document.createElement('input');
    target.value = value;
    const event = new Event('input');
    Object.defineProperty(event, 'target', { value: target });

    return { event, target };
}

type SliderState = {
    key?: 'protein' | 'fats' | 'carbs' | 'fiber';
    labelKey: string;
    unit: string;
    max: number;
    value: number;
    percent: number;
    accent: string;
    gradient: string;
};

type GoalsFacadeMock = {
    minCalories: number;
    maxCalories: number;
    calorieTarget: WritableSignal<number>;
    isLoadingGoals: WritableSignal<boolean>;
    isSavingGoals: WritableSignal<boolean>;
    hasLoadError: WritableSignal<boolean>;
    saveStatusKey: Signal<string | null>;
    macroPresets: MacroPreset[];
    selectedPreset: WritableSignal<'custom' | 'classic'>;
    waterState: WritableSignal<SliderState>;
    coreMacroStates: WritableSignal<SliderState[]>;
    fiberMacroState: WritableSignal<SliderState | undefined>;
    progressPercent: WritableSignal<number>;
    knobAngle: WritableSignal<number>;
    accentColor: WritableSignal<string>;
    calorieCyclingEnabled: WritableSignal<boolean>;
    dayCalories: WritableSignal<Record<DayCalorieKey, number>>;
    bodyTargetValues: WritableSignal<{ weight: number; waist: number }>;
    initialize: ReturnType<typeof vi.fn>;
    reload: ReturnType<typeof vi.fn>;
    toggleCalorieCycling: ReturnType<typeof vi.fn>;
    updateDayCalories: ReturnType<typeof vi.fn>;
    updateBodyTarget: ReturnType<typeof vi.fn>;
    normalizeCaloriesInput: ReturnType<typeof vi.fn>;
    updateCalories: ReturnType<typeof vi.fn>;
    changeMacroPreset: ReturnType<typeof vi.fn>;
    updateMacroValue: ReturnType<typeof vi.fn>;
    updateWaterValue: ReturnType<typeof vi.fn>;
};

function createFacadeMock(): GoalsFacadeMock {
    return {
        minCalories: 0,
        maxCalories: NORMALIZED_CALORIES,
        calorieTarget: signal(CALORIE_TARGET),
        isLoadingGoals: signal(false),
        isSavingGoals: signal(false),
        hasLoadError: signal(false),
        saveStatusKey: computed(() => null),
        macroPresets: [
            { key: 'custom', labelKey: 'GOALS_PAGE.MACRO_PRESET_CUSTOM' },
            { key: 'classic', labelKey: 'GOALS_PAGE.MACRO_PRESET_CLASSIC', percent: { protein: 0.3, fats: 0.3, carbs: 0.4 } },
        ],
        selectedPreset: signal<'custom' | 'classic'>('custom'),
        waterState: signal(createSliderState({ labelKey: 'GOALS_PAGE.WATER_LABEL', unit: 'ml', value: WATER_TARGET })),
        coreMacroStates: signal([createSliderState({ key: 'protein', labelKey: 'GOALS_PAGE.MACROS.PROTEIN', value: PROTEIN_TARGET })]),
        fiberMacroState: signal(createSliderState({ key: 'fiber', labelKey: 'GOALS_PAGE.MACROS.FIBER', value: 30 })),
        progressPercent: signal(PROGRESS_PERCENT),
        knobAngle: signal(KNOB_ANGLE),
        accentColor: signal('var(--fd-color-green-500)'),
        calorieCyclingEnabled: signal(true),
        dayCalories: signal({
            mondayCalories: CALORIE_TARGET,
            tuesdayCalories: CALORIE_TARGET,
            wednesdayCalories: CALORIE_TARGET,
            thursdayCalories: CALORIE_TARGET,
            fridayCalories: CALORIE_TARGET,
            saturdayCalories: CALORIE_TARGET,
            sundayCalories: CALORIE_TARGET,
        }),
        bodyTargetValues: signal({ weight: BODY_TARGET, waist: 80 }),
        initialize: vi.fn(),
        reload: vi.fn(),
        toggleCalorieCycling: vi.fn(),
        updateDayCalories: vi.fn(),
        updateBodyTarget: vi.fn(() => CLAMPED_BODY_TARGET),
        normalizeCaloriesInput: vi.fn(() => NORMALIZED_CALORIES),
        updateCalories: vi.fn(),
        changeMacroPreset: vi.fn(),
        updateMacroValue: vi.fn(() => CLAMPED_PROTEIN_TARGET),
        updateWaterValue: vi.fn(() => CLAMPED_WATER_TARGET),
    };
}

function createSliderState(overrides: Partial<SliderState>): SliderState {
    return {
        labelKey: 'GOALS_PAGE.MACROS.PROTEIN',
        unit: 'g',
        max: 220,
        value: 0,
        percent: 50,
        accent: 'var(--fd-color-green-500)',
        gradient: 'linear-gradient(90deg, green, red)',
        ...overrides,
    };
}

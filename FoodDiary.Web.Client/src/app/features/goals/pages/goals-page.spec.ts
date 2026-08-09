import { computed, type Signal, signal, type WritableSignal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { GoalsFacade, type MacroPreset } from '../lib/goals.facade';
import type { DayCalorieKey } from '../models/goals.data';
import { GoalsEditorComponent } from './goals-editor/goals-editor';
import { GoalsPageComponent } from './goals-page';

const CALORIE_TARGET = 2100;
const WATER_TARGET = 2200;
const BODY_WEIGHT = 72;
const FIBER_TARGET = 30;
const PROTEIN_TARGET = 150;

let facade: GoalsFacadeMock;

describe('GoalsPageComponent', () => {
    beforeEach(async () => {
        facade = createFacadeMock();

        await TestBed.configureTestingModule({
            imports: [GoalsPageComponent],
            providers: [provideTranslateTesting()],
        })
            .overrideComponent(GoalsPageComponent, {
                set: { providers: [{ provide: GoalsFacade, useValue: facade }] },
            })
            .compileComponents();
    });

    it('initializes goals and renders the editor as the only goals form', () => {
        const fixture = createComponent();

        expect(facade.initialize).toHaveBeenCalledTimes(1);
        expect(getEditor(fixture).calories()).toBe(CALORIE_TARGET);
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('fd-goals-calorie-card')).toBeNull();
        expect(element.querySelector('fd-goals-macros-card')).toBeNull();
    });

    it('renders load error and delegates retry', () => {
        facade.hasLoadError.set(true);
        const fixture = createComponent();

        fixture.debugElement.query(By.css('fd-error-state')).triggerEventHandler('retry');

        expect(facade.reload).toHaveBeenCalledTimes(1);
    });

    it('delegates editor save to the facade', () => {
        const fixture = createComponent();
        const request = { dailyCalorieTarget: CALORIE_TARGET };

        getEditor(fixture).save.emit(request);

        expect(facade.saveManually).toHaveBeenCalledWith(request);
    });
});

function createComponent(): ComponentFixture<GoalsPageComponent> {
    const fixture = TestBed.createComponent(GoalsPageComponent);
    fixture.detectChanges();
    return fixture;
}

function getEditor(fixture: ComponentFixture<GoalsPageComponent>): GoalsEditorComponent {
    return fixture.debugElement.query(By.directive(GoalsEditorComponent)).componentInstance as GoalsEditorComponent;
}

type MacroState = {
    key: 'protein' | 'fats' | 'carbs' | 'fiber';
    labelKey: string;
    unit: string;
    max: number;
    value: number;
    percent: number;
    accent: string;
    gradient: string;
};

type GoalsFacadeMock = {
    calorieTarget: WritableSignal<number>;
    isLoadingGoals: WritableSignal<boolean>;
    isSavingGoals: WritableSignal<boolean>;
    hasLoadError: WritableSignal<boolean>;
    saveStatusKey: Signal<string | null>;
    macroPresets: MacroPreset[];
    selectedPreset: WritableSignal<'custom'>;
    waterState: WritableSignal<{ value: number }>;
    macroStates: WritableSignal<MacroState[]>;
    calorieCyclingEnabled: WritableSignal<boolean>;
    dayCalories: WritableSignal<Record<DayCalorieKey, number>>;
    bodyTargetValues: WritableSignal<{ weight: number; waist: number }>;
    initialize: ReturnType<typeof vi.fn>;
    reload: ReturnType<typeof vi.fn>;
    saveManually: ReturnType<typeof vi.fn>;
};

function createFacadeMock(): GoalsFacadeMock {
    const days = {
        mondayCalories: CALORIE_TARGET,
        tuesdayCalories: CALORIE_TARGET,
        wednesdayCalories: CALORIE_TARGET,
        thursdayCalories: CALORIE_TARGET,
        fridayCalories: CALORIE_TARGET,
        saturdayCalories: CALORIE_TARGET,
        sundayCalories: CALORIE_TARGET,
    };

    return {
        calorieTarget: signal(CALORIE_TARGET),
        isLoadingGoals: signal(false),
        isSavingGoals: signal(false),
        hasLoadError: signal(false),
        saveStatusKey: computed(() => null),
        macroPresets: [{ key: 'custom', labelKey: 'GOALS_PAGE.MACRO_PRESET_CUSTOM' }],
        selectedPreset: signal<'custom'>('custom'),
        waterState: signal({ value: WATER_TARGET }),
        macroStates: signal([createMacroState('protein'), createMacroState('fiber')]),
        calorieCyclingEnabled: signal(false),
        dayCalories: signal(days),
        bodyTargetValues: signal({ weight: BODY_WEIGHT, waist: 0 }),
        initialize: vi.fn(),
        reload: vi.fn(),
        saveManually: vi.fn(),
    };
}

function createMacroState(key: MacroState['key']): MacroState {
    return {
        key,
        labelKey: `GOALS_PAGE.MACROS.${key.toUpperCase()}`,
        unit: 'g',
        max: 220,
        value: key === 'fiber' ? FIBER_TARGET : PROTEIN_TARGET,
        percent: 50,
        accent: 'var(--fd-color-green-500)',
        gradient: 'linear-gradient(90deg, green, red)',
    };
}

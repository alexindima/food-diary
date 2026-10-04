import { computed, type Signal, signal, type WritableSignal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { UnsavedChangesService } from '../../../services/unsaved-changes.service';
import type { DayCalorieKey } from '../../../shared/models/goals.data';
import { GoalsFacade, type MacroPreset } from '../lib/goals.facade';
import { createDayCalories } from '../lib/goals-state.mapper';
import { GoalsEditorComponent } from './goals-editor/goals-editor';
import { GoalsPageComponent } from './goals-page';

const CALORIE_TARGET = 2100;
const WATER_TARGET = 2200;
const BODY_WEIGHT = 72;
const FIBER_TARGET = 30;
const PROTEIN_TARGET = 150;
const RETRY_WATER_TARGET = 2500;
const NEWER_WATER_TARGET = 3000;
const EDITED_CALORIE_TARGET = 2500;

let facade: GoalsFacadeMock;

describe('GoalsPageComponent', () => {
    beforeEach(setupGoalsPageAsync);

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

        void getEditor(fixture).saveRequest()(request);

        expect(facade.saveManuallyAsync).toHaveBeenCalledWith(request);
    });
});

describe('GoalsPageComponent calorie cycling', () => {
    beforeEach(setupGoalsPageAsync);

    it.each([CALORIE_TARGET, 0])(
        'initializes an empty cycling week from the %s calorie goal without saving automatically',
        async calories => {
            facade.calorieTarget.set(calories);
            facade.dayCalories.set(createDayCalories(0));
            const fixture = createComponent();
            const element = fixture.nativeElement as HTMLElement;
            const toggle = element.querySelector<HTMLInputElement>('#goals-editor-cycling-enabled');
            toggle?.dispatchEvent(new Event('change'));
            fixture.detectChanges();
            const handler = TestBed.inject(UnsavedChangesService).getHandler();

            const inputs = [...element.querySelectorAll<HTMLInputElement>('fd-goals-cycling-day input')];
            expect(inputs.map(input => Number(input.value))).toEqual(Array.from({ length: 7 }, () => calories));
            expect(handler?.hasChanges()).toBe(true);
            expect(facade.saveManuallyAsync).not.toHaveBeenCalled();
            expect(await handler?.save()).toBe(true);
            expect(facade.saveManuallyAsync).toHaveBeenCalledWith(
                expect.objectContaining({ calorieCyclingEnabled: true, ...createDayCalories(calories) }),
            );
        },
    );

    it('initializes cycling from the unsaved calorie draft and discards the initialization with that draft', () => {
        facade.dayCalories.set(createDayCalories(0));
        const fixture = createComponent();
        const editor = getEditor(fixture);
        editor['updateCalories'](EDITED_CALORIE_TARGET);
        editor['updateCycling'](true);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const handler = TestBed.inject(UnsavedChangesService).getHandler();

        expect(element.querySelector<HTMLInputElement>('#goals-editor-cycling-mondayCalories')?.value).toBe(String(EDITED_CALORIE_TARGET));
        handler?.discard();
        fixture.detectChanges();
        expect(handler?.hasChanges()).toBe(false);
        expect(element.querySelector<HTMLInputElement>('#goals-editor-cycling-enabled')?.checked).toBe(false);
        expect(element.querySelector('fd-goals-cycling-day')).toBeNull();
        expect(facade.saveManuallyAsync).not.toHaveBeenCalled();
    });

    it('preserves a previously configured cycling week, including zero calorie days, when cycling is enabled again', async () => {
        const days = { ...createDayCalories(0), mondayCalories: CALORIE_TARGET, sundayCalories: EDITED_CALORIE_TARGET };
        facade.dayCalories.set(days);
        const fixture = createComponent();
        const editor = getEditor(fixture);
        editor['updateCycling'](true);
        editor['updateCycling'](false);
        editor['updateCycling'](true);

        expect(await TestBed.inject(UnsavedChangesService).getHandler()?.save()).toBe(true);
        expect(facade.saveManuallyAsync).toHaveBeenCalledWith(expect.objectContaining({ calorieCyclingEnabled: true, ...days }));
    });
});

describe('GoalsPageComponent saving drafts', () => {
    beforeEach(setupGoalsPageAsync);

    it('preserves unsaved goals after failure and permits a successful retry', async () => {
        facade.saveManuallyAsync.mockResolvedValueOnce(false).mockResolvedValueOnce(true);
        const fixture = createComponent();
        getEditor(fixture)['updateWater'](RETRY_WATER_TARGET);
        fixture.detectChanges();
        const handler = TestBed.inject(UnsavedChangesService).getHandler();

        expect(await handler?.save()).toBe(false);
        fixture.detectChanges();
        expect(handler?.hasChanges()).toBe(true);
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('fd-unsaved-changes-bar')).not.toBeNull();

        expect(await handler?.save()).toBe(true);
        fixture.detectChanges();
        expect(handler?.hasChanges()).toBe(false);
        expect(facade.saveManuallyAsync).toHaveBeenCalledTimes(2);
        expect(facade.saveManuallyAsync).toHaveBeenLastCalledWith(expect.objectContaining({ waterGoal: RETRY_WATER_TARGET }));
    });

    it('waits for saving and retains edits made while the request is pending', async () => {
        let completeSave: (saved: boolean) => void = () => {};
        facade.saveManuallyAsync.mockReturnValue(
            new Promise<boolean>(resolve => {
                completeSave = resolve;
            }),
        );
        const fixture = createComponent();
        const editor = getEditor(fixture);
        editor['updateWater'](RETRY_WATER_TARGET);
        const handler = TestBed.inject(UnsavedChangesService).getHandler();
        const firstSave = handler?.save();
        const repeatedSave = handler?.save();
        editor['updateWater'](NEWER_WATER_TARGET);
        completeSave(true);

        expect(await firstSave).toBe(false);
        expect(await repeatedSave).toBe(false);
        expect(handler?.hasChanges()).toBe(true);
        expect(facade.saveManuallyAsync).toHaveBeenCalledTimes(1);
    });
});

describe('GoalsPageComponent numeric validation', () => {
    beforeEach(setupGoalsPageAsync);

    it.each(['calories', 'protein', 'fiber', 'water'])('rejects a negative %s value before saving and permits correction', async field => {
        const fixture = createComponent();
        const element = fixture.nativeElement as HTMLElement;
        const input = element.querySelector<HTMLInputElement>(`#goals-editor-${field}`);
        if (input === null) {
            throw new Error(`Missing ${field} input`);
        }
        input.value = '-1';
        input.dispatchEvent(new Event('input'));
        fixture.detectChanges();
        const handler = TestBed.inject(UnsavedChangesService).getHandler();

        expect(await handler?.save()).toBe(false);
        expect(facade.saveManuallyAsync).not.toHaveBeenCalled();
        expect(handler?.hasChanges()).toBe(true);
        expect(input.getAttribute('aria-invalid')).toBe('true');
        expect(input.getAttribute('aria-describedby')).toBe(`goals-editor-${field}-error`);
        expect(element.querySelector(`#goals-editor-${field}-error`)).not.toBeNull();
        const buttons = element.querySelectorAll<HTMLButtonElement>('fd-unsaved-changes-bar button');
        expect(buttons[1].disabled).toBe(true);

        input.value = '0';
        input.dispatchEvent(new Event('input'));
        fixture.detectChanges();
        expect(input.getAttribute('aria-invalid')).toBe('false');
        expect(element.querySelector(`#goals-editor-${field}-error`)).toBeNull();
        expect(await handler?.save()).toBe(true);
        expect(facade.saveManuallyAsync).toHaveBeenCalledTimes(1);
    });
});

async function setupGoalsPageAsync(): Promise<void> {
    facade = createFacadeMock();

    await TestBed.configureTestingModule({
        imports: [GoalsPageComponent],
        providers: [provideTranslateTesting()],
    })
        .overrideComponent(GoalsPageComponent, {
            set: { providers: [{ provide: GoalsFacade, useValue: facade }] },
        })
        .compileComponents();
}

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
    saveManuallyAsync: ReturnType<typeof vi.fn>;
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
        saveManuallyAsync: vi.fn().mockResolvedValue(true),
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

import { signal, type WritableSignal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { FastingCheckInCardComponent } from './fasting-check-in-card';

const INITIAL_HUNGER_LEVEL = 1;
const INITIAL_ENERGY_LEVEL = 2;
const DEFAULT_CHECK_IN_LEVEL = 3;
const UPDATED_HUNGER_LEVEL = 4;
const UPDATED_ENERGY_LEVEL = 5;
const UPDATED_MOOD_LEVEL = 2;
const CHECK_IN_NOTES_LIMIT = 500;

describe('FastingCheckInCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingCheckInCardComponent],
            providers: [provideTranslateTesting()],
        });
    });

    it('updates writable signals from the rendered check-in controls', () => {
        const hungerLevel = signal(INITIAL_HUNGER_LEVEL);
        const energyLevel = signal(INITIAL_ENERGY_LEVEL);
        const moodLevel = signal(DEFAULT_CHECK_IN_LEVEL);
        const selectedSymptoms = signal<string[]>([]);
        const notes = signal('');
        const fixture = createComponent({ hungerLevel, energyLevel, moodLevel, selectedSymptoms, notes });
        const element = getElement(fixture);

        getButtonByLabel(element, `FASTING.CHECK_IN.HUNGER_LEVEL_${UPDATED_HUNGER_LEVEL}`).click();
        getButtonByLabel(element, `FASTING.CHECK_IN.ENERGY_LEVEL_${UPDATED_ENERGY_LEVEL}`).click();
        getButtonByLabel(element, `FASTING.CHECK_IN.MOOD_LEVEL_${UPDATED_MOOD_LEVEL}`).click();
        getButtonByLabel(element, 'FASTING.CHECK_IN.SYMPTOMS.HEADACHE').click();
        const notesInput = getNotesInput(element);
        notesInput.value = 'Feeling okay';
        notesInput.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        expect(hungerLevel()).toBe(UPDATED_HUNGER_LEVEL);
        expect(energyLevel()).toBe(UPDATED_ENERGY_LEVEL);
        expect(moodLevel()).toBe(UPDATED_MOOD_LEVEL);
        expect(selectedSymptoms()).toEqual(['headache']);
        expect(notes()).toBe('Feeling okay');

        notesInput.value = '';
        notesInput.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();

        expect(notes()).toBe('');
    });

    it('disables the draft when another terminal action is running', () => {
        const fixture = createComponent({ isUpdatingCycle: true });

        expect(getElement(fixture).querySelector('fieldset')?.disabled).toBe(true);
    });

    it('allows a full 500-character note while limiting native input to the API boundary', () => {
        const notes = signal('x'.repeat(CHECK_IN_NOTES_LIMIT));
        const fixture = createComponent({ notes });
        const input = getNotesInput(getElement(fixture));

        expect(input.maxLength).toBe(CHECK_IN_NOTES_LIMIT);
        expect(input.value).toBe(notes());
        expect(notes()).toHaveLength(CHECK_IN_NOTES_LIMIT);
    });

    it('provides descriptive hints for emoji scales and symptom chips', () => {
        const fixture = createComponent();
        const emojiPicker = fixture.debugElement.query(By.css('fd-ui-emoji-picker')).componentInstance as OptionsHost;
        const symptomChips = fixture.debugElement.queryAll(By.css('fd-ui-chip-select'))[1].componentInstance as OptionsHost;

        expect(emojiPicker.options()[0]).toMatchObject({
            ariaLabel: 'FASTING.CHECK_IN.HUNGER_LEVEL_1. FASTING.CHECK_IN.HUNGER_LEVEL_1_DESCRIPTION',
            hint: 'FASTING.CHECK_IN.HUNGER_LEVEL_1. FASTING.CHECK_IN.HUNGER_LEVEL_1_DESCRIPTION',
        });
        expect(symptomChips.options()[0]).toMatchObject({
            ariaLabel: 'FASTING.CHECK_IN.SYMPTOMS.HEADACHE. FASTING.CHECK_IN.SYMPTOM_HINTS.HEADACHE',
            hint: 'FASTING.CHECK_IN.SYMPTOM_HINTS.HEADACHE',
        });
    });

    it('keeps the no-symptoms choice mutually exclusive with warning symptoms', () => {
        const selectedSymptoms = signal<string[]>(['headache']);
        const fixture = createComponent({ selectedSymptoms });
        const element = getElement(fixture);

        getButtonByLabel(element, 'FASTING.CHECK_IN.SYMPTOMS.GOOD').click();
        fixture.detectChanges();
        expect(selectedSymptoms()).toEqual(['good']);

        getButtonByLabel(element, 'FASTING.CHECK_IN.SYMPTOMS.DIZZINESS').click();
        expect(selectedSymptoms()).toEqual(['dizziness']);
    });
});

type OptionsHost = {
    options: () => Array<Record<string, unknown>>;
};

type CheckInCardInput = {
    isSaving: boolean;
    isEnding: boolean;
    isUpdatingCycle: boolean;
    hungerLevel: WritableSignal<number>;
    energyLevel: WritableSignal<number>;
    moodLevel: WritableSignal<number>;
    selectedSymptoms: WritableSignal<string[]>;
    notes: WritableSignal<string>;
};

function createComponent(overrides: Partial<CheckInCardInput> = {}): ComponentFixture<FastingCheckInCardComponent> {
    const input: CheckInCardInput = {
        isSaving: false,
        isEnding: false,
        isUpdatingCycle: false,
        hungerLevel: signal(DEFAULT_CHECK_IN_LEVEL),
        energyLevel: signal(DEFAULT_CHECK_IN_LEVEL),
        moodLevel: signal(DEFAULT_CHECK_IN_LEVEL),
        selectedSymptoms: signal([]),
        notes: signal(''),
        ...overrides,
    };
    const fixture = TestBed.createComponent(FastingCheckInCardComponent);
    for (const [name, value] of Object.entries(input)) {
        fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingCheckInCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getButtonByLabel(element: HTMLElement, label: string): HTMLButtonElement {
    const button = element.querySelector<HTMLButtonElement>(`button[aria-label^="${label}."]`);
    if (button === null) {
        throw new Error(`Button with label "${label}" was not found.`);
    }

    return button;
}

function getNotesInput(element: HTMLElement): HTMLTextAreaElement {
    const input = element.querySelector('textarea');
    if (input === null) {
        throw new Error('Expected the check-in notes field');
    }

    return input;
}

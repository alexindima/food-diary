import { signal, type WritableSignal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../services/localization.service';
import type { FastingCheckInViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingCheckInCardComponent } from './fasting-check-in-card.component';

const INITIAL_HUNGER_LEVEL = 1;
const INITIAL_ENERGY_LEVEL = 2;
const DEFAULT_CHECK_IN_LEVEL = 3;
const UPDATED_HUNGER_LEVEL = 4;
const UPDATED_ENERGY_LEVEL = 5;
const UPDATED_MOOD_LEVEL = 2;

describe('FastingCheckInCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingCheckInCardComponent, TranslateModule.forRoot()],
            providers: [
                {
                    provide: LocalizationService,
                    useValue: {
                        getCurrentLanguage: vi.fn(() => 'en'),
                    },
                },
            ],
        });
    });

    it('renders nothing while fasting is inactive', () => {
        const fixture = createComponent({ isActive: false });

        expect(getElement(fixture).textContent.trim()).toBe('');
    });

    it('renders collapsed summary and emits form-open action', () => {
        const fixture = createComponent({ latestCheckIn: createCheckInViewModel() });
        const formOpen = vi.fn();
        fixture.componentInstance.formOpen.subscribe(formOpen);
        const element = getElement(fixture);

        expect(element.textContent).toContain('FASTING.CHECK_IN.UPDATE_ACTION');
        expect(element.textContent).toContain('Latest check-in');
        getButtonByText(element, 'FASTING.CHECK_IN.UPDATE_ACTION').click();

        expect(formOpen).toHaveBeenCalledTimes(1);
    });

    it('updates writable signals and emits save/close actions from expanded form', () => {
        const hungerLevel = signal(INITIAL_HUNGER_LEVEL);
        const energyLevel = signal(INITIAL_ENERGY_LEVEL);
        const moodLevel = signal(DEFAULT_CHECK_IN_LEVEL);
        const selectedSymptoms = signal<string[]>([]);
        const notes = signal('');
        const fixture = createComponent({
            isExpanded: true,
            hungerLevel,
            energyLevel,
            moodLevel,
            selectedSymptoms,
            notes,
        });
        const formClose = vi.fn();
        const save = vi.fn();
        fixture.componentInstance.formClose.subscribe(formClose);
        fixture.componentInstance.save.subscribe(save);

        const emojiPickers = fixture.debugElement.queryAll(By.css('fd-ui-emoji-picker'));
        emojiPickers[0].triggerEventHandler('selectedValueChange', UPDATED_HUNGER_LEVEL);
        emojiPickers[1].triggerEventHandler('selectedValueChange', UPDATED_ENERGY_LEVEL);
        emojiPickers[2].triggerEventHandler('selectedValueChange', UPDATED_MOOD_LEVEL);
        fixture.debugElement.query(By.css('fd-ui-chip-select')).triggerEventHandler('selectedValuesChange', ['headache']);
        fixture.debugElement.query(By.css('textarea')).triggerEventHandler('ngModelChange', 'Feeling okay');
        fixture.detectChanges();

        const element = getElement(fixture);
        getButtonByText(element, 'FASTING.CHECK_IN.HIDE_ACTION').click();
        getButtonByText(element, 'FASTING.CHECK_IN.SAVE').click();

        expect(hungerLevel()).toBe(UPDATED_HUNGER_LEVEL);
        expect(energyLevel()).toBe(UPDATED_ENERGY_LEVEL);
        expect(moodLevel()).toBe(UPDATED_MOOD_LEVEL);
        expect(selectedSymptoms()).toEqual(['headache']);
        expect(notes()).toBe('Feeling okay');
        expect(formClose).toHaveBeenCalledTimes(1);
        expect(save).toHaveBeenCalledTimes(1);
    });

    it('disables the draft when another terminal action is running', () => {
        const fixture = createComponent({ isExpanded: true, isUpdatingCycle: true });

        expect((fixture.componentInstance as unknown as { draftDisabled: () => boolean }).draftDisabled()).toBe(true);
    });
});

type CheckInCardInput = {
    isActive: boolean;
    isSaving: boolean;
    isEnding: boolean;
    isUpdatingCycle: boolean;
    isExpanded: boolean;
    latestCheckIn: FastingCheckInViewModel | null;
    hungerLevel: WritableSignal<number>;
    energyLevel: WritableSignal<number>;
    moodLevel: WritableSignal<number>;
    selectedSymptoms: WritableSignal<string[]>;
    notes: WritableSignal<string>;
};

function createComponent(overrides: Partial<CheckInCardInput> = {}): ComponentFixture<FastingCheckInCardComponent> {
    const input: CheckInCardInput = {
        isActive: true,
        isSaving: false,
        isEnding: false,
        isUpdatingCycle: false,
        isExpanded: false,
        latestCheckIn: null,
        hungerLevel: signal(DEFAULT_CHECK_IN_LEVEL),
        energyLevel: signal(DEFAULT_CHECK_IN_LEVEL),
        moodLevel: signal(DEFAULT_CHECK_IN_LEVEL),
        selectedSymptoms: signal([]),
        notes: signal(''),
        ...overrides,
    };
    const fixture = TestBed.createComponent(FastingCheckInCardComponent);
    fixture.componentRef.setInput('isActive', input.isActive);
    fixture.componentRef.setInput('isSaving', input.isSaving);
    fixture.componentRef.setInput('isEnding', input.isEnding);
    fixture.componentRef.setInput('isUpdatingCycle', input.isUpdatingCycle);
    fixture.componentRef.setInput('isExpanded', input.isExpanded);
    fixture.componentRef.setInput('latestCheckIn', input.latestCheckIn);
    fixture.componentRef.setInput('hungerLevel', input.hungerLevel);
    fixture.componentRef.setInput('energyLevel', input.energyLevel);
    fixture.componentRef.setInput('moodLevel', input.moodLevel);
    fixture.componentRef.setInput('selectedSymptoms', input.selectedSymptoms);
    fixture.componentRef.setInput('notes', input.notes);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingCheckInCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function getButtonByText(element: HTMLElement, text: string): HTMLElement {
    const button = Array.from(element.querySelectorAll<HTMLElement>('fd-ui-button')).find(item => item.textContent.includes(text));
    if (button === undefined) {
        throw new Error(`Button with text "${text}" was not found.`);
    }

    return button;
}

function createCheckInViewModel(): FastingCheckInViewModel {
    return {
        checkIn: {
            id: 'check-in-1',
            checkedInAtUtc: '2026-05-16T10:00:00.000Z',
            hungerLevel: 3,
            energyLevel: 4,
            moodLevel: 5,
            symptoms: [],
            notes: null,
        },
        checkedInAtLabel: '10:00',
        relativeCheckedInAt: '5 minutes ago',
        summary: 'Latest check-in',
        symptomLabels: [],
    };
}

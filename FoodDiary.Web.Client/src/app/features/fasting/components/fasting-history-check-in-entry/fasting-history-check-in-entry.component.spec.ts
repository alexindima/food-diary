import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import type { FastingCheckInViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingHistoryCheckInEntryComponent } from './fasting-history-check-in-entry.component';

describe('FastingHistoryCheckInEntryComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingHistoryCheckInEntryComponent],
        });
    });

    it('renders check-in details', () => {
        const fixture = createComponent(createCheckInViewModel());
        const text = getElement(fixture).textContent;

        expect(text).toContain('10:30');
        expect(text).toContain('Hungry but focused');
        expect(text).toContain('Dizziness');
        expect(text).toContain('Short walk helped');
    });

    it('hides optional symptoms and notes when they are missing', () => {
        const fixture = createComponent(
            createCheckInViewModel({
                symptomLabels: [],
                checkIn: { ...createCheckInViewModel().checkIn, notes: null },
            }),
        );
        const element = getElement(fixture);

        expect(element.querySelector('.fasting__history-symptoms')).toBeNull();
        expect(element.textContent).not.toContain('Short walk helped');
    });
});

function createComponent(checkIn: FastingCheckInViewModel): ComponentFixture<FastingHistoryCheckInEntryComponent> {
    const fixture = TestBed.createComponent(FastingHistoryCheckInEntryComponent);
    fixture.componentRef.setInput('checkIn', checkIn);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingHistoryCheckInEntryComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createCheckInViewModel(overrides: Partial<FastingCheckInViewModel> = {}): FastingCheckInViewModel {
    return {
        checkIn: {
            id: 'check-in-1',
            checkedInAtUtc: '2026-05-16T10:30:00.000Z',
            hungerLevel: 2,
            energyLevel: 3,
            moodLevel: 4,
            symptoms: ['dizziness'],
            notes: 'Short walk helped',
        },
        checkedInAtLabel: '10:30',
        relativeCheckedInAt: null,
        summary: 'Hungry but focused',
        symptomLabels: ['Dizziness'],
        ...overrides,
    };
}

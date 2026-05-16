import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import type { FastingCheckInViewModel } from '../../pages/fasting-page-lib/fasting-page.types';
import { FastingCheckInSummaryComponent } from './fasting-check-in-summary.component';

describe('FastingCheckInSummaryComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingCheckInSummaryComponent, TranslateModule.forRoot()],
        });
    });

    it('renders nothing without a latest check-in', () => {
        const fixture = createComponent(null);

        expect(getElement(fixture).textContent.trim()).toBe('');
    });

    it('renders summary, symptoms, and notes for the latest check-in', () => {
        const fixture = createComponent(createCheckInViewModel());
        const text = getElement(fixture).textContent;

        expect(text).toContain('FASTING.CHECK_IN.LAST_SAVED');
        expect(text).toContain('Balanced check-in');
        expect(text).toContain('Headache');
        expect(text).toContain('Mild fatigue');
    });
});

function createComponent(latestCheckIn: FastingCheckInViewModel | null): ComponentFixture<FastingCheckInSummaryComponent> {
    const fixture = TestBed.createComponent(FastingCheckInSummaryComponent);
    fixture.componentRef.setInput('latestCheckIn', latestCheckIn);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingCheckInSummaryComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createCheckInViewModel(): FastingCheckInViewModel {
    return {
        checkIn: {
            id: 'check-in-1',
            checkedInAtUtc: '2026-05-16T10:00:00.000Z',
            hungerLevel: 3,
            energyLevel: 4,
            moodLevel: 5,
            symptoms: ['headache'],
            notes: 'Mild fatigue',
        },
        checkedInAtLabel: '10:00',
        relativeCheckedInAt: '5 minutes ago',
        summary: 'Balanced check-in',
        symptomLabels: ['Headache'],
    };
}

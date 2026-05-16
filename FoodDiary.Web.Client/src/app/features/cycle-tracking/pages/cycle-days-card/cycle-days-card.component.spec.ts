import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import type { CycleDayViewModel } from '../cycle-tracking-page-lib/cycle-tracking-page.types';
import { CycleDaysCardComponent } from './cycle-days-card.component';

const ITEMS: CycleDayViewModel[] = [
    {
        day: {
            id: 'day-1',
            cycleId: 'cycle-1',
            date: '2026-04-02T00:00:00.000Z',
            isPeriod: true,
            symptoms: {
                pain: 5,
                mood: 3,
                edema: 1,
                headache: 2,
                energy: 4,
                sleepQuality: 6,
                libido: 2,
            },
            notes: 'felt tired',
        },
        dateLabel: 'Apr 2, 2026',
        accentColor: 'var(--fd-color-red-600)',
        badgeLabelKey: 'CYCLE_TRACKING.BADGE_PERIOD',
    },
];

let fixture: ComponentFixture<CycleDaysCardComponent>;

beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [CycleDaysCardComponent, TranslateModule.forRoot()],
    });

    fixture = TestBed.createComponent(CycleDaysCardComponent);
});

describe('CycleDaysCardComponent', () => {
    it('shows loading state', () => {
        fixture.componentRef.setInput('isLoading', true);
        fixture.componentRef.setInput('items', []);
        fixture.detectChanges();

        expect(getText()).toContain('CYCLE_TRACKING.LOADING');
    });

    it('shows empty state when there are no days', () => {
        fixture.componentRef.setInput('isLoading', false);
        fixture.componentRef.setInput('items', []);
        fixture.detectChanges();

        expect(getText()).toContain('CYCLE_TRACKING.NO_DAYS');
    });

    it('renders day symptoms and notes', () => {
        fixture.componentRef.setInput('isLoading', false);
        fixture.componentRef.setInput('items', ITEMS);
        fixture.detectChanges();

        expect(getText()).toContain('Apr 2, 2026');
        expect(getText()).toContain('CYCLE_TRACKING.SYMPTOM_PAIN: 5/9');
        expect(getText()).toContain('CYCLE_TRACKING.SYMPTOM_SLEEP: 6/9');
        expect(getText()).toContain('felt tired');
    });
});

function getText(): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

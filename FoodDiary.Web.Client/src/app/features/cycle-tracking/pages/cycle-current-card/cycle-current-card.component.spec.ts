import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import type { CycleResponse } from '../../models/cycle.data';
import { CycleCurrentCardComponent } from './cycle-current-card.component';

const CYCLE: CycleResponse = {
    id: 'cycle-1',
    userId: 'user-1',
    startDate: '2026-04-01T00:00:00.000Z',
    averageLength: 28,
    lutealLength: 14,
    days: [],
    predictions: null,
};

let fixture: ComponentFixture<CycleCurrentCardComponent>;

beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [CycleCurrentCardComponent, TranslateModule.forRoot()],
    });

    fixture = TestBed.createComponent(CycleCurrentCardComponent);
});

describe('CycleCurrentCardComponent', () => {
    it('shows loading state', () => {
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();

        expect(getText()).toContain('CYCLE_TRACKING.LOADING');
    });

    it('derives empty title from missing current cycle', () => {
        fixture.componentRef.setInput('isLoading', false);
        fixture.componentRef.setInput('current', null);
        fixture.detectChanges();

        expect(fixture.componentInstance.titleKey()).toBe('CYCLE_TRACKING.NO_CYCLE');
        expect(getText()).toContain('CYCLE_TRACKING.NO_CYCLE_COPY');
    });

    it('renders current cycle and prediction blocks', () => {
        fixture.componentRef.setInput('isLoading', false);
        fixture.componentRef.setInput('current', {
            cycle: CYCLE,
            startDateLabel: 'Apr 1, 2026',
        });
        fixture.componentRef.setInput('prediction', {
            prediction: {
                nextPeriodStart: '2026-04-29',
                ovulationDate: '2026-04-15',
                pmsStart: '2026-04-23',
            },
            nextPeriodStartLabel: 'Apr 29',
            ovulationDateLabel: 'Apr 15',
            pmsStartLabel: 'Apr 23',
        });
        fixture.detectChanges();

        expect(fixture.componentInstance.titleKey()).toBe('CYCLE_TRACKING.CURRENT_CYCLE');
        expect(getText()).toContain('Apr 1, 2026');
        expect(getText()).toContain('Apr 29');
        expect(getText()).toContain('Apr 15');
        expect(getText()).toContain('Apr 23');
    });
});

function getText(): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

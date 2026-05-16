import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { CyclePredictions } from '../../../cycle-tracking/models/cycle.data';
import { CycleSummaryCardComponent } from './cycle-summary-card.component';

const CYCLE_DAY = 5;
const DAYS_TO_PERIOD = 24;

describe('CycleSummaryCardComponent', () => {
    it('calculates cycle day and next period status from inputs', async () => {
        const { component, fixture } = await setupComponentAsync({
            startDate: '2026-05-01',
            referenceDate: '2026-05-05',
            predictions: {
                ovulationDate: null,
                nextPeriodStart: '2026-05-29',
            },
        });

        fixture.detectChanges();

        expect(component.hasCycle()).toBe(true);
        expect(component.cycleDay()).toBe(CYCLE_DAY);
        expect(component.statusKey()).toBe('CYCLE_CARD.NEXT_PERIOD_IN');
        expect(component.statusDays()).toBe(DAYS_TO_PERIOD);
    });

    it('emits setup action when cycle data is missing', async () => {
        const { component } = await setupComponentAsync({ startDate: null, predictions: null });
        const setupSpy = vi.fn();
        component.setupAction.subscribe(setupSpy);

        expect(component.hasCycle()).toBe(false);
        component.onSetup();

        expect(setupSpy).toHaveBeenCalledOnce();
    });
});

async function setupComponentAsync(
    overrides: Partial<{
        startDate: string | null;
        predictions: CyclePredictions | null;
        referenceDate: Date | string | null;
        isLoading: boolean;
    }> = {},
): Promise<{
    component: CycleSummaryCardComponent;
    fixture: ComponentFixture<CycleSummaryCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [CycleSummaryCardComponent, TranslateModule.forRoot()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(CycleSummaryCardComponent);
    fixture.componentRef.setInput('startDate', 'startDate' in overrides ? overrides.startDate : '2026-05-01');
    fixture.componentRef.setInput('predictions', 'predictions' in overrides ? overrides.predictions : null);
    fixture.componentRef.setInput('referenceDate', 'referenceDate' in overrides ? overrides.referenceDate : '2026-05-05');
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

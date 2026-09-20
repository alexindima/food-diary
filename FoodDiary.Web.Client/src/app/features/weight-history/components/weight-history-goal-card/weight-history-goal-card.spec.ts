import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WeightHistoryGoalCardComponent } from './weight-history-goal-card';

const FIXTURE_CURRENT_MEASUREMENT = 78;
const FIXTURE_TARGET_MEASUREMENT = 75;
const FIXTURE_INITIAL_MEASUREMENT = 113;
const FIXTURE_UPPER_MEASUREMENT = 100;
const FIXTURE_REFERENCE_MEASUREMENT = 80;
const FIXTURE_SIGNED_LOSS = -35;
const FIXTURE_TOTAL_CHANGE = 35;
const FIXTURE_RECENT_MEASUREMENT = 90;
const FIXTURE_DECREASE = -10;

function setup(
    current: number | null = FIXTURE_CURRENT_MEASUREMENT,
    goal: number | null = FIXTURE_TARGET_MEASUREMENT,
    start: number | null = FIXTURE_INITIAL_MEASUREMENT,
): { fixture: ComponentFixture<WeightHistoryGoalCardComponent>; component: WeightHistoryGoalCardComponent; root: HTMLElement } {
    TestBed.configureTestingModule({ imports: [WeightHistoryGoalCardComponent], providers: [provideTranslateTesting()] });
    const fixture = TestBed.createComponent(WeightHistoryGoalCardComponent);
    for (const [name, value] of Object.entries({
        currentWeight: current,
        currentWeightDate: '2026-08-13',
        desiredWeightKg: goal,
        startWeightKg: start,
        startedAtUtc: '2026-08-06T00:00:00Z',
        hasGoalHistory: true,
        lastCompletedGoal: null,
    })) {
        fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, root: fixture.nativeElement as HTMLElement };
}
describe('Weight goal progress', () => {
    it.each([
        { start: 113, current: 78, goal: 75, change: -35, remaining: 3, percent: 92.1052631579 },
        { start: 70, current: 75, goal: 80, change: 5, remaining: 5, percent: 50 },
        { start: 100, current: 110, goal: 80, change: 10, remaining: 30, percent: 0 },
        { start: 100, current: 75, goal: 80, change: -25, remaining: 0, percent: 100 },
        { start: 80, current: 80, goal: 80, change: 0, remaining: 0, percent: 100 },
    ])('computes start $start/current $current/goal $goal', ({ start, current, goal, change, remaining, percent }) => {
        const { component, root } = setup(current, goal, start);
        expect(component['progress']()?.change).toBe(change);
        expect(component['progress']()?.remaining).toBe(remaining);
        expect(component['progress']()?.percent).toBeCloseTo(percent);
        const labels = Array.from(root.querySelectorAll('.weight-history-page__goal-stat .fd-ui-caption')).map(e => e.textContent);
        expect(labels).toEqual([
            'WEIGHT_HISTORY.GOAL_START_WEIGHT',
            'WEIGHT_HISTORY.GOAL_PERIOD_CHANGE',
            'WEIGHT_HISTORY.GOAL_CURRENT_WEIGHT',
        ]);
        const stats = root.querySelectorAll('.weight-history-page__goal-stat');
        expect(stats[1].textContent).toContain(change < 0 ? '−' : change > 0 ? '+' : '0');
    });
    it.each([
        [null, FIXTURE_TARGET_MEASUREMENT, FIXTURE_UPPER_MEASUREMENT],
        [FIXTURE_REFERENCE_MEASUREMENT, null, FIXTURE_UPPER_MEASUREMENT],
        [FIXTURE_REFERENCE_MEASUREMENT, FIXTURE_TARGET_MEASUREMENT, null],
    ])('does not fabricate progress with incomplete data', (current, goal, start) => {
        const { component, root } = setup(current, goal, start);
        expect(component['progress']()).toBeNull();
        expect(root.querySelector('[role="progressbar"]')).toBeNull();
    });
    it('renders no-goal state and emits actions from real buttons', () => {
        const { component, root } = setup(FIXTURE_REFERENCE_MEASUREMENT, null);
        const configure = vi.fn();
        const history = vi.fn();
        component.configureGoal.subscribe(configure);
        component.viewGoalHistory.subscribe(history);
        expect(root.textContent).toContain('WEIGHT_HISTORY.GOAL_NOT_SET');
        const buttons = Array.from(root.querySelectorAll('button'));
        buttons.find(b => b.textContent.includes('WEIGHT_HISTORY.SET_GOAL'))?.click();
        buttons.find(b => b.textContent.includes('WEIGHT_HISTORY.GOAL_HISTORY'))?.click();
        expect(configure).toHaveBeenCalledOnce();
        expect(history).toHaveBeenCalledOnce();
    });
});
describe('Goal estimates and completed goals', () => {
    it('renders imperial values while keeping canonical calculations', () => {
        const { fixture, component, root } = setup();
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        fixture.detectChanges();
        expect(component['progress']()?.change).toBe(FIXTURE_SIGNED_LOSS);
        expect(root.textContent).toContain('GENERAL.UNITS.LB');
    });
    it.each([null, 'not-a-date', '2026-08-20'])('does not estimate positive rate for missing/invalid/reversed dates %s', date => {
        const { fixture, component } = setup();
        fixture.componentRef.setInput('startedAtUtc', date);
        fixture.detectChanges();
        expect(component['progress']()?.weeklyRate).toBe(0);
        expect(component['progress']()?.daysToGoal).toBeNull();
    });
    it('handles a missing latest date and a valid weekly estimate', () => {
        const { fixture, component } = setup();
        expect(component['progress']()?.weeklyRate).toBe(FIXTURE_TOTAL_CHANGE);
        expect(component['progress']()?.daysToGoal).toBe(1);
        fixture.componentRef.setInput('currentWeightDate', null);
        fixture.detectChanges();
        expect(component['progress']()?.daysToGoal).toBeNull();
    });
    it.each([null, FIXTURE_RECENT_MEASUREMENT])('summarizes last goal without inventing a missing end measurement', end => {
        const { fixture, component, root } = setup(FIXTURE_REFERENCE_MEASUREMENT, null);
        fixture.componentRef.setInput('lastCompletedGoal', {
            id: 'g',
            targetWeightKg: 75,
            startWeightKg: 100,
            endWeightKg: end,
            startedAtUtc: '2026-01-01',
            endedAtUtc: '2026-02-01',
            status: 'Cancelled',
        });
        fixture.detectChanges();
        expect(component['lastGoalSummary']()?.change).toBe(end === null ? null : FIXTURE_DECREASE);
        expect(root.textContent).toContain('WEIGHT_HISTORY.LAST_GOAL');
    });
});

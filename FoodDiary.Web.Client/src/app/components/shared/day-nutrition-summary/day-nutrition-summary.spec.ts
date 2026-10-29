import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { DayNutritionSummaryComponent } from './day-nutrition-summary';

const originalScrollTo = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'scrollTo');
beforeAll(() => {
    Object.defineProperty(HTMLElement.prototype, 'scrollTo', { configurable: true, value: vi.fn() });
});
afterAll(() => {
    if (originalScrollTo === undefined) {
        Reflect.deleteProperty(HTMLElement.prototype, 'scrollTo');
    } else {
        Object.defineProperty(HTMLElement.prototype, 'scrollTo', originalScrollTo);
    }
});

function getElement(fixture: ComponentFixture<DayNutritionSummaryComponent>, selector: string): HTMLElement {
    const element = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Missing summary element: ${selector}`);
    }
    return element;
}

async function createSummaryAsync(): Promise<ComponentFixture<DayNutritionSummaryComponent>> {
    await TestBed.configureTestingModule({
        imports: [DayNutritionSummaryComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(DayNutritionSummaryComponent);
    fixture.componentRef.setInput('data', {
        mealCount: 1,
        dailyGoal: 2000,
        dailyConsumed: 500,
        weeklyConsumed: 0,
        weeklyGoal: null,
        nutrientBars: [{ id: 'protein', label: 'Protein', current: 20, target: 100, unit: 'g', colorStart: 'green', colorEnd: 'green' }],
    });
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
}

describe('Day summary empty state', () => {
    it.each([0, 1])('distinguishes %s meals with zero calories', async mealCount => {
        await TestBed.configureTestingModule({
            imports: [DayNutritionSummaryComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(DayNutritionSummaryComponent);
        fixture.componentRef.setInput('data', {
            mealCount,
            dailyGoal: 2000,
            dailyConsumed: 0,
            weeklyConsumed: 0,
            weeklyGoal: null,
            nutrientBars: [],
        });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain(
            mealCount === 0 ? 'DASHBOARD.DAY_SUMMARY.PULSE_EMPTY_TITLE' : 'DASHBOARD.DAY_SUMMARY.PULSE_CALORIES',
        );
        expect(host.textContent).not.toContain(
            mealCount === 0 ? 'DASHBOARD.DAY_SUMMARY.PULSE_CALORIES' : 'DASHBOARD.DAY_SUMMARY.PULSE_EMPTY_TITLE',
        );
    });
});

describe('Day summary keyboard access', () => {
    it('keeps public previews static and exposes keyboard metric actions only when interaction is enabled', async () => {
        const fixture = await createSummaryAsync();
        const host = fixture.nativeElement as HTMLElement;
        const details = vi.fn();
        fixture.componentInstance.details.subscribe(details);
        expect(host.querySelector('.day-summary__metric-action')).toBeNull();
        fixture.componentRef.setInput('interactive', true);
        fixture.detectChanges();
        const nutrient = getElement(fixture, '.day-summary__nutrient[role=button]');
        const calories = getElement(fixture, '.day-summary__calories[role=button]');
        expect(nutrient.getAttribute('aria-haspopup')).toBe('dialog');
        nutrient.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        const space = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
        calories.dispatchEvent(space);
        expect(space.defaultPrevented).toBe(true);
        expect(details.mock.calls).toEqual([['protein'], ['calories']]);
        fixture.componentRef.setInput('interactive', false);
        fixture.detectChanges();
        expect(host.querySelector('.day-summary__metric-action')).toBeNull();
    });

    it('makes the horizontally scrollable pulse reachable and named for keyboard users', async () => {
        const fixture = await createSummaryAsync();
        const viewport = getElement(fixture, '.day-summary__pulse-content');

        expect(viewport.tabIndex, 'The scrollable pulse needs a keyboard focus target').toBe(0);
        expect(viewport.getAttribute('aria-label')).toBe('DASHBOARD.DAY_SUMMARY.PULSE_TITLE');
    });
});

describe('Day summary pulse navigation', () => {
    it('synchronizes the counter and announcement with native scrolling in both directions', async () => {
        const fixture = await createSummaryAsync();
        const viewport = getElement(fixture, '.day-summary__pulse-content');
        const controls = getElement(fixture, '.day-summary__pulse-controls');
        const announcement = getElement(fixture, '[aria-live="polite"]');
        Object.defineProperty(viewport, 'clientWidth', { value: 300 });

        viewport.scrollLeft = 300;
        viewport.dispatchEvent(new Event('scroll'));
        fixture.detectChanges();
        expect(controls.textContent).toContain('2 / 2');
        expect(announcement.textContent).toContain('Protein');

        viewport.scrollLeft = 0;
        viewport.dispatchEvent(new Event('scroll'));
        fixture.detectChanges();
        expect(controls.textContent).toContain('1 / 2');
        expect(announcement.textContent).toContain('DASHBOARD.DAY_SUMMARY.PULSE_CALORIES');
    });

    it('keeps arrow navigation and wrapping aligned with the visible page', async () => {
        const fixture = await createSummaryAsync();
        const viewport = getElement(fixture, '.day-summary__pulse-content');
        const controls = getElement(fixture, '.day-summary__pulse-controls');
        Object.defineProperty(viewport, 'clientWidth', { value: 300 });
        const scroll = vi.spyOn(viewport, 'scrollTo').mockImplementation(options => {
            viewport.scrollLeft = (options as ScrollToOptions).left ?? 0;
            viewport.dispatchEvent(new Event('scroll'));
        });
        const [previous, next] = controls.querySelectorAll('button');

        previous.click();
        fixture.detectChanges();
        expect(scroll).toHaveBeenLastCalledWith({ left: 300 });
        expect(controls.textContent).toContain('2 / 2');

        next.click();
        fixture.detectChanges();
        expect(scroll).toHaveBeenLastCalledWith({ left: 0 });
        expect(controls.textContent).toContain('1 / 2');
    });

    it('returns to the first insight on refreshed data and hides navigation for a single insight', async () => {
        const fixture = await createSummaryAsync();
        const viewport = getElement(fixture, '.day-summary__pulse-content');
        const controls = getElement(fixture, '.day-summary__pulse-controls');
        const announcement = getElement(fixture, '[aria-live="polite"]');
        Object.defineProperty(viewport, 'clientWidth', { value: 300 });
        viewport.scrollLeft = 300;
        viewport.dispatchEvent(new Event('scroll'));
        fixture.detectChanges();
        const scroll = vi.spyOn(viewport, 'scrollTo');

        fixture.componentRef.setInput('data', {
            mealCount: 0,
            dailyGoal: 2000,
            dailyConsumed: 0,
            weeklyConsumed: 0,
            weeklyGoal: null,
            nutrientBars: [],
        });
        fixture.detectChanges();
        await fixture.whenStable();
        expect(scroll).toHaveBeenLastCalledWith({ left: 0, behavior: 'instant' });
        expect(controls.hidden).toBe(true);
        expect(controls.textContent).toContain('1 / 1');
        expect(announcement.textContent).toContain('DASHBOARD.DAY_SUMMARY.PULSE_EMPTY_TITLE');
        expect(viewport.children).toHaveLength(1);
    });
});

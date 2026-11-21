import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideChildTranslateService, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import type { NutrientBar } from '../nutrition-summary/nutrition-summary.types';
import { DayNutritionSummaryComponent } from './day-nutrition-summary';

const originalScrollTo = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'scrollTo');
const NO_GOAL_DATA = { dailyGoal: 0, dailyConsumed: 500, weeklyConsumed: 0, weeklyGoal: null, nutrientBars: null };
const SUMMARY_RECT = { x: 20, y: 30, width: 400, height: 400 };
const INITIAL_NUTRIENT_LABEL_RECT = { x: 460, y: 130, width: 100, height: 32 };
const MOVED_NUTRIENT_LABEL_RECT = { ...INITIAL_NUTRIENT_LABEL_RECT, y: 200 };
const PROTEIN_BAR = { id: 'protein', label: 'Protein', current: 20, target: 100, unit: 'g', colorStart: 'green', colorEnd: 'green' };
const SODIUM_BAR = { id: 'sodium', label: 'Sodium', current: 250, target: 500, unit: 'mg', colorStart: 'blue', colorEnd: 'blue' };
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

async function createSummaryAsync(nutrientBars: NutrientBar[] = [PROTEIN_BAR]): Promise<ComponentFixture<DayNutritionSummaryComponent>> {
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
        nutrientBars,
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

    it('keeps an empty diary prompt with a fiber goal until the diary contains a fiber entry', async () => {
        const fixture = await createSummaryAsync();
        const fiberGoal = {
            id: 'fiber',
            label: 'Fiber',
            current: 0,
            target: 25,
            unit: 'g',
            colorStart: 'purple',
            colorEnd: 'purple',
        };
        const diary = {
            mealCount: 0,
            dailyGoal: 2000,
            dailyConsumed: 0,
            weeklyConsumed: 0,
            weeklyGoal: null,
        };
        fixture.componentRef.setInput('data', { ...diary, nutrientBars: [fiberGoal] });
        fixture.detectChanges();

        const announcement = getElement(fixture, '[aria-live="polite"]');
        expect(announcement.textContent).toContain('DASHBOARD.DAY_SUMMARY.PULSE_EMPTY_TITLE');
        expect(getElement(fixture, '.day-summary__nutrient--fiber').textContent).toContain('Fiber');

        fixture.componentRef.setInput('data', { ...diary, nutrientBars: [{ ...fiberGoal, current: 5 }] });
        fixture.detectChanges();

        expect(announcement.textContent).toContain('DASHBOARD.DAY_SUMMARY.PULSE_CALORIES');
        expect(getElement(fixture, '.day-summary__pulse-content').textContent).toContain('Fiber');
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

describe('Day summary user interaction and missing goals', () => {
    it('activates only interactive metrics and ignores held key repeats', async () => {
        const fixture = await createSummaryAsync();
        const details = vi.fn();
        fixture.componentInstance.details.subscribe(details);
        const nutrient = getElement(fixture, '.day-summary__nutrient--protein');
        nutrient.click();
        nutrient.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        expect(details).not.toHaveBeenCalled();

        fixture.componentRef.setInput('interactive', true);
        fixture.detectChanges();
        nutrient.click();
        const repeat = new KeyboardEvent('keydown', { key: 'Enter', repeat: true, bubbles: true, cancelable: true });
        nutrient.dispatchEvent(repeat);
        expect(repeat.defaultPrevented).toBe(true);
        expect(details).toHaveBeenCalledExactlyOnceWith('protein');
    });

    it('shows logged calories without filling a goal ring when no goal exists', async () => {
        const fixture = await createSummaryAsync();
        fixture.componentRef.setInput('data', NO_GOAL_DATA);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.day-summary__calorie-value')?.textContent.trim()).toBe('500');
        expect(host.querySelector('.day-summary__calorie-ring')?.getAttribute('stroke-dashoffset')).toBe('100');
        expect(host.querySelectorAll('.day-summary__nutrient')).toHaveLength(0);
        expect(host.querySelector('.day-summary__pulse-content')?.textContent).toContain('DASHBOARD.DAY_SUMMARY.PULSE_CALORIES');
    });
});

describe('Day summary nutrient connectors', () => {
    it('connects measured core nutrients while keeping extra nutrients visible without a connector', async () => {
        const observers: SummaryResizeObserver[] = [];
        class SummaryResizeObserver implements ResizeObserver {
            public readonly observe = vi.fn<(target: Element) => void>();
            public readonly unobserve = vi.fn<(target: Element) => void>();
            public readonly disconnect = vi.fn();

            public constructor(private readonly callback: ResizeObserverCallback) {
                observers.push(this);
            }

            public notify(): void {
                this.callback([], this);
            }
        }
        vi.stubGlobal('ResizeObserver', SummaryResizeObserver);

        try {
            const fixture = await createSummaryAsync([PROTEIN_BAR, SODIUM_BAR]);
            const host = fixture.nativeElement as HTMLElement;
            const chart = getElement(fixture, '.day-summary__visual');
            const layout = getElement(fixture, '.day-summary__layout');
            const protein = getElement(fixture, '.day-summary__nutrient--protein');
            const observer = observers.find(candidate => candidate.observe.mock.calls.some(([observed]) => observed === chart));
            if (observer === undefined) {
                throw new Error('Expected a resize observer for the nutrient contour');
            }
            expect(observer.observe).toHaveBeenCalledWith(protein);
            expect(observer.observe).toHaveBeenCalledWith(layout);
            expect(host.querySelector('.day-summary__connector')).toBeNull();

            vi.spyOn(chart, 'getBoundingClientRect').mockReturnValue(DOMRect.fromRect(SUMMARY_RECT));
            const labelBounds = vi.spyOn(protein, 'getBoundingClientRect').mockReturnValue(DOMRect.fromRect(INITIAL_NUTRIENT_LABEL_RECT));
            observer.notify();
            fixture.detectChanges();
            expect(getElement(fixture, '.day-summary__nutrient--sodium').textContent).toMatch(/Sodium\s+250\s*\/\s*500\s+mg/u);
            expect(host.querySelectorAll('.day-summary__connector')).toHaveLength(1);
            const connector = host.querySelector('.day-summary__connector');
            if (connector === null) {
                throw new Error('Expected a connector after the chart becomes measurable');
            }
            const firstLabelEndY = connector.getAttribute('y1');
            const anchorY = connector.getAttribute('y2');
            expect(firstLabelEndY).not.toBeNull();
            expect(anchorY).not.toBeNull();
            expect(Number.isFinite(Number(firstLabelEndY))).toBe(true);
            expect(Number.isFinite(Number(anchorY))).toBe(true);

            labelBounds.mockReturnValue(DOMRect.fromRect(MOVED_NUTRIENT_LABEL_RECT));
            observer.notify();
            fixture.detectChanges();
            expect(connector.getAttribute('y1')).not.toBe(firstLabelEndY);
            expect(connector.getAttribute('y2')).toBe(anchorY);

            fixture.componentRef.setInput('data', { ...fixture.componentInstance.data(), nutrientBars: [PROTEIN_BAR] });
            fixture.detectChanges();
            await fixture.whenStable();
            const nextObserver = observers.at(-1);
            if (nextObserver === undefined || nextObserver === observer) {
                throw new Error('Expected a replacement observer after the nutrient update');
            }
            expect(observer.disconnect).toHaveBeenCalledOnce();
            expect(nextObserver.observe.mock.calls).toEqual([[chart], [layout], [protein]]);
            expect(host.querySelector('.day-summary__nutrient--sodium')).toBeNull();

            fixture.destroy();
            expect(nextObserver.disconnect).toHaveBeenCalledOnce();
        } finally {
            vi.unstubAllGlobals();
        }
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

it('keeps the selected nutrient visible and announces its translation after a locale switch', async () => {
    await TestBed.configureTestingModule({
        imports: [DayNutritionSummaryComponent],
        providers: [provideTranslateTesting({ lang: 'en' })],
    })
        .overrideComponent(DayNutritionSummaryComponent, { add: { providers: provideChildTranslateService() } })
        .compileComponents();
    const applicationTranslations = TestBed.inject(TranslateService);
    applicationTranslations.setTranslation('en', {
        GENERAL: { NUTRIENTS: { PROTEIN: 'Proteins' }, UNITS: { G: 'g' } },
        DASHBOARD: { DAY_SUMMARY: { REMAINING: '{{value}} {{unit}} remaining' } },
    });
    applicationTranslations.setTranslation('ru', {
        GENERAL: { NUTRIENTS: { PROTEIN: 'Белки' }, UNITS: { G: 'г' } },
        DASHBOARD: { DAY_SUMMARY: { REMAINING: 'Осталось {{value}} {{unit}}' } },
    });

    const fixture = TestBed.createComponent(DayNutritionSummaryComponent);
    const widgetTranslations = fixture.debugElement.injector.get(TranslateService);
    widgetTranslations.setTranslation('en', { GENERAL: { NUTRIENTS: { PROTEIN: 'Protein' } } });
    widgetTranslations.setTranslation('ru', { GENERAL: { NUTRIENTS: { PROTEIN: 'Белок' } } });
    fixture.componentRef.setInput('data', {
        ...NO_GOAL_DATA,
        nutrientBars: [{ ...PROTEIN_BAR, labelKey: 'GENERAL.NUTRIENTS.PROTEIN', unitKey: 'GENERAL.UNITS.G' }],
    });
    fixture.detectChanges();

    const viewport = getElement(fixture, '.day-summary__pulse-content');
    const controls = getElement(fixture, '.day-summary__pulse-controls');
    const announcement = getElement(fixture, '[aria-live="polite"]');
    const slideWidth = 300;
    Object.defineProperty(viewport, 'clientWidth', { value: slideWidth });
    viewport.scrollLeft = slideWidth;
    viewport.dispatchEvent(new Event('scroll'));
    fixture.detectChanges();
    expect(controls.textContent).toContain('2 / 2');
    expect(announcement.textContent).toMatch(/Protein\s*·\s*80 g remaining/u);

    await firstValueFrom(applicationTranslations.use('ru'));
    fixture.detectChanges();

    expect(viewport.scrollLeft).toBe(slideWidth);
    expect(controls.textContent).toContain('2 / 2');
    expect(announcement.textContent).toMatch(/Белок\s*·\s*Осталось 80 г/u);
    expect(viewport.children.item(1)?.textContent).toMatch(/Белок\s*·\s*Осталось 80 г/u);
    expect(applicationTranslations.instant('GENERAL.NUTRIENTS.PROTEIN')).toBe('Белки');
});

describe('Day summary nutrition and geometry fallbacks', () => {
    it('shows both the reached calorie goal and the exceeded nutrient target', async () => {
        const insightCount = 3;
        const targetCalories = 2_000;
        const proteinTarget = 80;
        const proteinConsumed = 90;
        const consumedFiber = 10;
        const fiberTarget = 25;
        const fixture = await createSummaryAsync();
        fixture.componentRef.setInput('data', {
            mealCount: 1,
            dailyGoal: targetCalories,
            dailyConsumed: targetCalories,
            weeklyConsumed: targetCalories,
            weeklyGoal: null,
            nutrientBars: [
                {
                    id: 'protein',
                    label: 'Protein',
                    current: proteinConsumed,
                    target: proteinTarget,
                    unit: 'g',
                    colorStart: 'green',
                    colorEnd: 'green',
                },
                {
                    id: 'fiber',
                    label: 'Fiber',
                    current: consumedFiber,
                    target: fiberTarget,
                    unit: 'g',
                    colorStart: 'purple',
                    colorEnd: 'purple',
                },
            ],
        });
        fixture.detectChanges();

        const pulse = getElement(fixture, '.day-summary__pulse-content');
        expect(getElement(fixture, '[aria-live="polite"]').textContent).toContain('DASHBOARD.DAY_SUMMARY.TARGET_REACHED');
        expect(pulse.children).toHaveLength(insightCount);
        expect(pulse.children[1].textContent).toContain('Protein');
        expect(pulse.children[1].textContent).toContain('DASHBOARD.DAY_SUMMARY.EXCEEDED_BY');
        expect(pulse.children[2].textContent).toContain('Fiber');
        expect(pulse.children[2].textContent).toContain('DASHBOARD.DAY_SUMMARY.REMAINING');
    });

    it('keeps the active announcement while a responsive pulse has no measurable width', async () => {
        const viewportWidth = 300;
        const fixture = await createSummaryAsync();
        const viewport = getElement(fixture, '.day-summary__pulse-content');
        const liveRegion = getElement(fixture, '[aria-live="polite"]');
        Object.defineProperty(viewport, 'clientWidth', { configurable: true, value: viewportWidth });
        viewport.scrollLeft = viewportWidth;
        viewport.dispatchEvent(new Event('scroll'));
        fixture.detectChanges();
        expect(liveRegion.textContent).toContain('Protein');

        Object.defineProperty(viewport, 'clientWidth', { configurable: true, value: 0 });
        viewport.scrollLeft = 0;
        viewport.dispatchEvent(new Event('scroll'));
        fixture.detectChanges();

        expect(liveRegion.textContent).toContain('Protein');
        Object.defineProperty(viewport, 'clientWidth', { configurable: true, value: viewportWidth });
        viewport.dispatchEvent(new Event('scroll'));
        fixture.detectChanges();
        expect(liveRegion.textContent).toContain('DASHBOARD.DAY_SUMMARY.PULSE_CALORIES');
    });
});

describe('Day summary browser geometry capability', () => {
    it('renders nutrition and keeps metric controls usable when ResizeObserver is unavailable', async () => {
        vi.stubGlobal('ResizeObserver', undefined);
        try {
            const fixture = await createSummaryAsync();
            const details = vi.fn();
            fixture.componentInstance.details.subscribe(details);
            fixture.componentRef.setInput('interactive', true);
            fixture.detectChanges();

            expect((fixture.nativeElement as HTMLElement).querySelector('.day-summary__connector')).toBeNull();
            expect(getElement(fixture, '.day-summary__calorie-value').textContent.trim()).toBe('500');
            getElement(fixture, '.day-summary__nutrient--protein').click();
            expect(details).toHaveBeenCalledExactlyOnceWith('protein');
        } finally {
            vi.unstubAllGlobals();
        }
    });
});

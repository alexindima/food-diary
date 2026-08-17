import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../src/testing/async-testing';
import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiCalendarComponent } from './fd-ui-calendar';

const TEST_YEAR = 2025;
const MARCH_INDEX = 2;
const APRIL_INDEX = 3;
const CALENDAR_WEEKS_COUNT = 6;
const CALENDAR_DAYS_COUNT = 42;
const WEEK_DAYS_COUNT = 7;
const BEFORE_MIN_DAY = 9;
const MIN_DAY = 10;
const TEST_DAY = 15;
const MAX_DAY = 20;
const MARCH_DATE = new Date(TEST_YEAR, MARCH_INDEX, TEST_DAY);

let component: FdUiCalendarComponent;
let fixture: ComponentFixture<FdUiCalendarComponent>;

const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
const requireElement = (selector: string): HTMLElement => {
    const element = host().querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected element ${selector} to exist.`);
    }

    return element;
};

describe('FdUiCalendarComponent', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [FdUiCalendarComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        fixture = TestBed.createComponent(FdUiCalendarComponent);
        component = fixture.componentInstance;
        component.displayMonth.set(MARCH_DATE);
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    registerRenderTests();
    registerSelectionTests();
    registerNavigationTests();
    registerWeekSelectionTests();
});

function weekdayLabels(): Array<string | null> {
    return Array.from(host().querySelectorAll('.fd-ui-calendar__weekday')).map(label => label.textContent.trim());
}

function registerWeekSelectionTests(): void {
    describe('week selection', () => {
        it('normalizes the selected value to Monday and highlights the whole week', () => {
            fixture.componentRef.setInput('selectionMode', 'week');
            fixture.detectChanges();

            component['selectDate'](MARCH_DATE);
            fixture.detectChanges();

            expect(component.value()?.getDate()).toBe(MIN_DAY);
            expect(host().querySelectorAll('.fd-ui-calendar__day--week-selected').length).toBe(WEEK_DAYS_COUNT);
        });

        it('keeps standalone appearance by default and supports embedded appearance', () => {
            expect(requireElement('.fd-ui-calendar').classList).not.toContain('fd-ui-calendar--embedded');

            fixture.componentRef.setInput('appearance', 'embedded');
            fixture.detectChanges();

            expect(requireElement('.fd-ui-calendar').classList).toContain('fd-ui-calendar--embedded');
        });
    });
}

function registerRenderTests(): void {
    describe('rendering', () => {
        it('should render six calendar weeks', () => {
            const rows = host().querySelectorAll('.fd-ui-calendar__week');
            const cells = host().querySelectorAll('.fd-ui-calendar__day');

            expect(rows.length).toBe(CALENDAR_WEEKS_COUNT);
            expect(cells.length).toBe(CALENDAR_DAYS_COUNT);
        });

        it('should render weekday labels starting on Monday by default', () => {
            const labels = weekdayLabels();

            expect(labels[0]).toBe('Mon');
            expect(labels[CALENDAR_WEEKS_COUNT]).toBe('Sun');
        });

        it('should render weekday labels starting on Sunday when configured', () => {
            fixture.componentRef.setInput('weekStartsOn', 0);
            fixture.detectChanges();

            const labels = weekdayLabels();

            expect(labels[0]).toBe('Sun');
            expect(labels[CALENDAR_WEEKS_COUNT]).toBe('Sat');
        });

        it('should render translated month navigation labels', () => {
            const controls = Array.from(host().querySelectorAll<HTMLButtonElement>('.fd-ui-calendar__header button'));

            expect(controls[0].getAttribute('aria-label')).toBe('CALENDAR.PREVIOUS_MONTH');
            expect(controls[1].getAttribute('aria-label')).toBe('CALENDAR.NEXT_MONTH');
        });

        it('should render date markers and include their labels in the accessible date name', () => {
            fixture.componentRef.setInput('markers', [
                { date: '2025-03-15', tone: 'danger', label: 'Logged bleeding' },
                { date: '2025-03-15', tone: 'danger', label: 'Logged bleeding' },
                { date: '2025-03-15', tone: 'warning', label: 'Predicted period' },
            ]);
            fixture.detectChanges();

            const markedDate = requireElement('[data-date="2025-03-15"]');
            expect(markedDate.querySelectorAll('.fd-ui-calendar__marker').length).toBe(2);
            expect(markedDate.getAttribute('aria-label')).toContain('Logged bleeding');
            expect(markedDate.getAttribute('aria-label')).toContain('Predicted period');
            expect(markedDate.getAttribute('aria-label')?.match(/Logged bleeding/g)).toHaveLength(1);
        });
    });
}

function registerSelectionTests(): void {
    describe('selection', () => {
        it('should select enabled date and mark it selected', () => {
            component['selectDate'](MARCH_DATE);
            fixture.detectChanges();

            expect(component.value()?.getFullYear()).toBe(TEST_YEAR);
            expect(component.value()?.getMonth()).toBe(MARCH_INDEX);
            expect(component.value()?.getDate()).toBe(TEST_DAY);

            const selected = requireElement('[data-date="2025-03-15"]');
            expect(selected.classList).toContain('fd-ui-calendar__day--selected');
            expect(selected.getAttribute('aria-selected')).toBe('true');
        });

        it('should ignore date earlier than min', () => {
            fixture.componentRef.setInput('min', new Date(TEST_YEAR, MARCH_INDEX, MIN_DAY));
            fixture.detectChanges();

            component['selectDate'](new Date(TEST_YEAR, MARCH_INDEX, BEFORE_MIN_DAY));

            expect(component.value()).toBeNull();
        });

        it('should disable dates outside configured range', () => {
            fixture.componentRef.setInput('min', new Date(TEST_YEAR, MARCH_INDEX, MIN_DAY));
            fixture.componentRef.setInput('max', new Date(TEST_YEAR, MARCH_INDEX, MAX_DAY));
            fixture.detectChanges();

            const beforeMin = requireElement('[data-date="2025-03-09"]') as HTMLButtonElement;
            const inRange = requireElement('[data-date="2025-03-15"]') as HTMLButtonElement;
            const afterMax = requireElement('[data-date="2025-03-21"]') as HTMLButtonElement;

            expect(beforeMin.disabled).toBe(true);
            expect(inRange.disabled).toBe(false);
            expect(afterMax.disabled).toBe(true);
        });
    });
}

function registerNavigationTests(): void {
    describe('navigation', () => {
        it('should move display month with header controls', () => {
            component['showNextMonth']();
            expect(component.displayMonth()?.getMonth()).toBe(APRIL_INDEX);

            component['showPreviousMonth']();
            expect(component.displayMonth()?.getMonth()).toBe(MARCH_INDEX);
        });

        it('should select date from enter key', () => {
            const event = new KeyboardEvent('keydown', { key: 'Enter' });
            const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

            component['onCellKeydown'](event, MARCH_DATE);

            expect(preventDefaultSpy).toHaveBeenCalled();
            expect(component.value()?.getDate()).toBe(TEST_DAY);
        });

        it('should navigate with keyboard and clamp to min date', async () => {
            fixture.componentRef.setInput('min', new Date(TEST_YEAR, MARCH_INDEX, MIN_DAY));
            fixture.detectChanges();
            const event = new KeyboardEvent('keydown', { key: 'ArrowLeft' });
            const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

            component['onCellKeydown'](event, new Date(TEST_YEAR, MARCH_INDEX, MIN_DAY));
            fixture.detectChanges();
            await waitForAsyncTasksAsync();

            expect(preventDefaultSpy).toHaveBeenCalled();
            const active = requireElement('[data-date="2025-03-10"]');
            expect(active.classList).toContain('fd-ui-calendar__day--active');
        });

        it('should change visible month when keyboard navigation leaves current month', () => {
            const event = new KeyboardEvent('keydown', { key: 'PageDown' });

            component['onCellKeydown'](event, MARCH_DATE);

            expect(component.displayMonth()?.getMonth()).toBe(APRIL_INDEX);
        });
    });
}

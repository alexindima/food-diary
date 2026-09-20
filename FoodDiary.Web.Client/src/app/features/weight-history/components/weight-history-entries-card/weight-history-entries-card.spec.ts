import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import type { WeightEntry } from '../../models/weight-entry.data';
import { WeightHistoryEntriesCardComponent } from './weight-history-entries-card';

const ENTRY_WEIGHT = 71.5;
const OLDER_ENTRY_WEIGHT = 74.5;
const DEFAULT_DESIRED_WEIGHT = 70;
const GAIN_DESIRED_WEIGHT = 90;
const EXPECTED_WEIGHT_CHANGE = '−3';

describe('WeightHistoryEntriesCardComponent', () => {
    it('renders empty state when entries list is empty', () => {
        const { fixture } = setupComponent([]);

        expect(getText(fixture)).toContain('WEIGHT_HISTORY.NO_ENTRIES');
    });

    it('builds entry view models inside the component', () => {
        const { component, fixture } = setupComponent([createEntry()]);

        expect(component['items']()).toEqual([
            {
                entry: createEntry(),
                dateLabel: '05/15/2026',
                isToday: false,
                change: null,
                tone: 'neutral',
            },
        ]);
        expect(getText(fixture)).toContain(String(ENTRY_WEIGHT));
    });

    it('emits entry actions', () => {
        const entry = createEntry();
        const { component, fixture } = setupComponent([entry]);
        const editHandler = vi.fn();
        const removeHandler = vi.fn();
        const showAllHandler = vi.fn();
        component['editEntry'].subscribe(editHandler);
        component['removeEntry'].subscribe(removeHandler);
        component['showAllEntries'].subscribe(showAllHandler);

        const root = fixture.nativeElement as HTMLElement;
        root.querySelector<HTMLButtonElement>('button[aria-label="WEIGHT_HISTORY.EDIT"]')?.click();
        root.querySelector<HTMLButtonElement>('button[aria-label="WEIGHT_HISTORY.DELETE"]')?.click();
        component['showAllEntries'].emit();

        expect(editHandler).toHaveBeenCalledWith(entry);
        expect(removeHandler).toHaveBeenCalledWith(entry);
        expect(showAllHandler).toHaveBeenCalledOnce();
    });

    it('renders weight loss as negative when the goal requires weight gain', () => {
        const latestEntry = createEntry();
        const olderEntry = { ...createEntry(), id: 'entry-2', date: '2026-05-14T00:00:00Z', weightKg: OLDER_ENTRY_WEIGHT };
        const { fixture } = setupComponent([latestEntry, olderEntry], GAIN_DESIRED_WEIGHT);
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('.weight-history-page__entry-change--gain')?.textContent).toContain(EXPECTED_WEIGHT_CHANGE);
    });
});

function setupComponent(
    entries: WeightEntry[],
    desiredWeightKg = DEFAULT_DESIRED_WEIGHT,
): {
    component: WeightHistoryEntriesCardComponent;
    fixture: ComponentFixture<WeightHistoryEntriesCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WeightHistoryEntriesCardComponent],
        providers: [provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(WeightHistoryEntriesCardComponent);
    fixture.componentRef.setInput('isLoading', false);
    fixture.componentRef.setInput('entries', entries);
    fixture.componentRef.setInput('currentWeight', ENTRY_WEIGHT);
    fixture.componentRef.setInput('desiredWeightKg', desiredWeightKg);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WeightHistoryEntriesCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createEntry(): WeightEntry {
    return {
        id: 'entry-1',
        userId: 'user-1',
        date: '2026-05-15T00:00:00Z',
        weightKg: ENTRY_WEIGHT,
    };
}

describe('Recent entries visibility and pagination', () => {
    it.each([
        { count: 0, visible: 0, more: false },
        { count: 1, visible: 1, more: false },
        { count: 5, visible: 5, more: false },
        { count: 6, visible: 5, more: true },
    ])('shows $visible of $count records', ({ count, visible, more }) => {
        const entries = Array.from({ length: count }, (_, index) => ({ ...createEntry(), id: `entry-${index}`, weightKg: index + 1 }));
        const { component, fixture } = setupComponent(entries);
        const showAll = vi.fn();
        component.showAllEntries.subscribe(showAll);
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll('.weight-history-page__entry')).toHaveLength(visible);
        const toggle = root.querySelector<HTMLButtonElement>('.weight-history-page__entries-toggle button');
        expect(toggle !== null).toBe(more);
        toggle?.click();
        expect(showAll).toHaveBeenCalledTimes(more ? 1 : 0);
        if (count > 1) {
            expect(component['items']()[0].change).toBe(-1);
        }
    });
    it('hides record actions while loading', () => {
        const { fixture } = setupComponent([createEntry()]);
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();
        expect(getText(fixture)).toContain('WEIGHT_HISTORY.LOADING');
        expect((fixture.nativeElement as HTMLElement).querySelector('button')).toBeNull();
    });
});

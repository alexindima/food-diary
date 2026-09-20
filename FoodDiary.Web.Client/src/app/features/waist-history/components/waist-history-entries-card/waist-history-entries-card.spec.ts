import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import type { WaistEntry } from '../../models/waist-entry.data';
import { WaistHistoryEntriesCardComponent } from './waist-history-entries-card';

const ENTRY_CIRCUMFERENCE = 81.5;

describe('WaistHistoryEntriesCardComponent', () => {
    it('renders empty state when entries list is empty', () => {
        const { fixture } = setupComponent([]);

        expect(getText(fixture)).toContain('WAIST_HISTORY.NO_ENTRIES');
    });

    it('builds entry view models inside the component', () => {
        const { component, fixture } = setupComponent([createEntry()]);

        expect(component['items']()).toEqual([
            {
                entry: createEntry(),
                dateLabel: '05/15/2026',
                isToday: false,
                change: null,
            },
        ]);
        expect(getText(fixture)).toContain(String(ENTRY_CIRCUMFERENCE));
    });

    it('emits entry actions', () => {
        const entry = createEntry();
        const { component, fixture } = setupComponent([entry]);
        const editHandler = vi.fn();
        const removeHandler = vi.fn();
        component['editEntry'].subscribe(editHandler);
        component['removeEntry'].subscribe(removeHandler);

        const root = fixture.nativeElement as HTMLElement;
        root.querySelector<HTMLButtonElement>('button[aria-label="WAIST_HISTORY.EDIT"]')?.click();
        root.querySelector<HTMLButtonElement>('button[aria-label="WAIST_HISTORY.DELETE"]')?.click();

        expect(editHandler).toHaveBeenCalledWith(entry);
        expect(removeHandler).toHaveBeenCalledWith(entry);
    });
});

function setupComponent(entries: WaistEntry[]): {
    component: WaistHistoryEntriesCardComponent;
    fixture: ComponentFixture<WaistHistoryEntriesCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WaistHistoryEntriesCardComponent],
        providers: [provideTranslateTesting()],
    });
    TestBed.inject(MeasurementSystemService).setSystem('metric');

    const fixture = TestBed.createComponent(WaistHistoryEntriesCardComponent);
    fixture.componentRef.setInput('isLoading', false);
    fixture.componentRef.setInput('entries', entries);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<WaistHistoryEntriesCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}

function createEntry(): WaistEntry {
    return {
        id: 'entry-1',
        userId: 'user-1',
        date: '2026-05-15T00:00:00Z',
        circumferenceCm: ENTRY_CIRCUMFERENCE,
    };
}

describe('Recent entries visibility and pagination', () => {
    it.each([
        { count: 0, visible: 0, more: false },
        { count: 1, visible: 1, more: false },
        { count: 5, visible: 5, more: false },
        { count: 6, visible: 5, more: true },
    ])('shows $visible of $count records', ({ count, visible, more }) => {
        const entries = Array.from({ length: count }, (_, index) => ({
            ...createEntry(),
            id: `entry-${index}`,
            circumferenceCm: index + 1,
        }));
        const { component, fixture } = setupComponent(entries);
        const showAll = vi.fn();
        component.showAllEntries.subscribe(showAll);
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll('.waist-history-page__entry')).toHaveLength(visible);
        const toggle = root.querySelector<HTMLButtonElement>('.waist-history-page__entries-toggle button');
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
        expect(getText(fixture)).toContain('WAIST_HISTORY.LOADING');
        expect((fixture.nativeElement as HTMLElement).querySelector('button')).toBeNull();
    });
});

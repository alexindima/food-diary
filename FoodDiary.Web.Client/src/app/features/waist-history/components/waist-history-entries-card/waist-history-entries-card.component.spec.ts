import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import type { WaistEntry } from '../../models/waist-entry.data';
import { WaistHistoryEntriesCardComponent } from './waist-history-entries-card.component';

const ENTRY_CIRCUMFERENCE = 81.5;

describe('WaistHistoryEntriesCardComponent', () => {
    it('renders empty state when entries list is empty', () => {
        const { fixture } = setupComponent([]);

        expect(getText(fixture)).toContain('WAIST_HISTORY.NO_ENTRIES');
    });

    it('builds entry view models inside the component', () => {
        const { component, fixture } = setupComponent([createEntry()]);

        expect(component.items()).toEqual([
            {
                entry: createEntry(),
                dateLabel: '05/15/2026',
            },
        ]);
        expect(getText(fixture)).toContain(String(ENTRY_CIRCUMFERENCE));
    });

    it('emits entry actions', () => {
        const entry = createEntry();
        const { component } = setupComponent([entry]);
        const editHandler = vi.fn();
        const removeHandler = vi.fn();
        component.editEntry.subscribe(editHandler);
        component.removeEntry.subscribe(removeHandler);

        component.editEntry.emit(entry);
        component.removeEntry.emit(entry);

        expect(editHandler).toHaveBeenCalledWith(entry);
        expect(removeHandler).toHaveBeenCalledWith(entry);
    });
});

function setupComponent(entries: WaistEntry[]): {
    component: WaistHistoryEntriesCardComponent;
    fixture: ComponentFixture<WaistHistoryEntriesCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [WaistHistoryEntriesCardComponent, TranslateModule.forRoot()],
    });

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
        circumference: ENTRY_CIRCUMFERENCE,
    };
}

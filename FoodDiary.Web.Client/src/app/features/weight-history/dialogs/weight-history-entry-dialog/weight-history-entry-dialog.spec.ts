import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WeightHistoryEntryDialogComponent } from './weight-history-entry-dialog';

const FIXTURE_INITIAL_VERSION = 7;
const FIXTURE_TARGET_MEASUREMENT = 75;
const createFacade = (): {
    form: FieldTree<{ date: string; weight: string }>;
    desiredWeightForm: FieldTree<{ date: string; weight: string }>;
    isSaving: WritableSignal<boolean>;
    isDesiredWeightSaving: WritableSignal<boolean>;
    isEditing: WritableSignal<boolean>;
    entryError: WritableSignal<string | null>;
    entrySaveVersion: WritableSignal<number>;
    desiredWeightSaveVersion: WritableSignal<number>;
    desiredWeightKg: WritableSignal<number | null>;
    cancelEdit: Mock;
    saveDesiredWeight: Mock;
    cancelWeightGoal: Mock;
} => {
    const model = signal({ date: '2026-04-01', weight: '80' });
    const fields = form(model);
    return {
        form: fields,
        desiredWeightForm: fields,
        isSaving: signal(false),
        isDesiredWeightSaving: signal(false),
        isEditing: signal(false),
        entryError: signal<string | null>(null),
        entrySaveVersion: signal(FIXTURE_INITIAL_VERSION),
        desiredWeightSaveVersion: signal(FIXTURE_INITIAL_VERSION),
        desiredWeightKg: signal<number | null>(FIXTURE_TARGET_MEASUREMENT),
        cancelEdit: vi.fn(),
        saveDesiredWeight: vi.fn(),
        cancelWeightGoal: vi.fn(),
    } as const;
};

function setup(): {
    fixture: ComponentFixture<WeightHistoryEntryDialogComponent>;
    component: WeightHistoryEntryDialogComponent;
    facade: ReturnType<typeof createFacade>;
    close: Mock;
} {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [WeightHistoryEntryDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: WeightHistoryFacade, useFactory: createFacade },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const facade = TestBed.inject(WeightHistoryFacade) as unknown as ReturnType<typeof createFacade>;
    const fixture = TestBed.createComponent(WeightHistoryEntryDialogComponent);
    fixture.detectChanges();
    return { fixture, facade, component: fixture.componentInstance, close };
}
describe('Weight entry dialog lifecycle', () => {
    it('does not close for an old save or loading-state changes', () => {
        const { fixture, facade, close } = setup();
        facade.isSaving.set(true);
        fixture.detectChanges();
        facade.isSaving.set(false);
        fixture.detectChanges();
        expect(close).not.toHaveBeenCalled();
    });
    it('closes only when a new successful save arrives', () => {
        const { fixture, facade, close } = setup();
        facade.entrySaveVersion.update(v => v + 1);
        fixture.detectChanges();
        expect(close).toHaveBeenCalledOnce();
    });
    it('unsubscribes the save effect when destroyed', () => {
        const { fixture, facade, close } = setup();
        fixture.destroy();
        facade.entrySaveVersion.update(v => v + 1);
        TestBed.tick();
        expect(close).not.toHaveBeenCalled();
    });
    it.each([false, true])('closes and resets editing only when editing=%s', editing => {
        const { fixture, facade, component, close } = setup();
        facade.isEditing.set(editing);
        fixture.detectChanges();
        component['close']();
        expect(close).toHaveBeenCalledOnce();
        expect(facade.cancelEdit).toHaveBeenCalledTimes(editing ? 1 : 0);
    });
    it('renders server errors and retains the dialog', () => {
        const { fixture, facade, close } = setup();
        facade.entryError.set('Duplicate date');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('Duplicate date');
        expect(close).not.toHaveBeenCalled();
    });
});

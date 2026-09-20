import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import { WaistHistoryGoalDialogComponent } from './waist-history-goal-dialog';

const FIXTURE_INITIAL_VERSION = 7;
const FIXTURE_TARGET_MEASUREMENT = 75;
const createFacade = (): {
    form: FieldTree<{ date: string; circumference: string }>;
    desiredWaistForm: FieldTree<{ date: string; circumference: string }>;
    isSaving: WritableSignal<boolean>;
    isDesiredWaistSaving: WritableSignal<boolean>;
    isEditing: WritableSignal<boolean>;
    entryError: WritableSignal<string | null>;
    entrySaveVersion: WritableSignal<number>;
    desiredWaistSaveVersion: WritableSignal<number>;
    desiredWaistCm: WritableSignal<number | null>;
    cancelEdit: Mock;
    saveDesiredWaist: Mock;
    cancelWaistGoal: Mock;
} => {
    const model = signal({ date: '2026-04-01', circumference: '80' });
    const fields = form(model);
    return {
        form: fields,
        desiredWaistForm: fields,
        isSaving: signal(false),
        isDesiredWaistSaving: signal(false),
        isEditing: signal(false),
        entryError: signal<string | null>(null),
        entrySaveVersion: signal(FIXTURE_INITIAL_VERSION),
        desiredWaistSaveVersion: signal(FIXTURE_INITIAL_VERSION),
        desiredWaistCm: signal<number | null>(FIXTURE_TARGET_MEASUREMENT),
        cancelEdit: vi.fn(),
        saveDesiredWaist: vi.fn(),
        cancelWaistGoal: vi.fn(),
    } as const;
};

function setup(): {
    fixture: ComponentFixture<WaistHistoryGoalDialogComponent>;
    component: WaistHistoryGoalDialogComponent;
    facade: ReturnType<typeof createFacade>;
    close: Mock;
} {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [WaistHistoryGoalDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: WaistHistoryFacade, useFactory: createFacade },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const facade = TestBed.inject(WaistHistoryFacade) as unknown as ReturnType<typeof createFacade>;
    const fixture = TestBed.createComponent(WaistHistoryGoalDialogComponent);
    fixture.detectChanges();
    return { fixture, facade, component: fixture.componentInstance, close };
}
describe('Waist goal dialog lifecycle', () => {
    it('does not close for an old save or loading-state changes', () => {
        const { fixture, facade, close } = setup();
        facade.isDesiredWaistSaving.set(true);
        fixture.detectChanges();
        facade.isDesiredWaistSaving.set(false);
        fixture.detectChanges();
        expect(close).not.toHaveBeenCalled();
    });
    it('closes only when a new successful save arrives', () => {
        const { fixture, facade, close } = setup();
        facade.desiredWaistSaveVersion.update(v => v + 1);
        fixture.detectChanges();
        expect(close).toHaveBeenCalledOnce();
    });
    it('unsubscribes the save effect when destroyed', () => {
        const { fixture, facade, close } = setup();
        fixture.destroy();
        facade.desiredWaistSaveVersion.update(v => v + 1);
        TestBed.tick();
        expect(close).not.toHaveBeenCalled();
    });
    it('delegates save and cancellation through real buttons', () => {
        const { fixture, facade } = setup();
        const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
        buttons.find(b => b.textContent.includes('WAIST_HISTORY.START_NEW_GOAL'))?.click();
        expect(facade.saveDesiredWaist).toHaveBeenCalledOnce();
        buttons.find(b => b.textContent.includes('WAIST_HISTORY.CANCEL_GOAL'))?.click();
        expect(facade.cancelWaistGoal).toHaveBeenCalledOnce();
    });
    it('offers creation with no active goal and closes explicitly', () => {
        const { fixture, facade, component, close } = setup();
        facade.desiredWaistCm.set(null);
        fixture.detectChanges();
        const text = (fixture.nativeElement as HTMLElement).textContent;
        expect(text).toContain('WAIST_HISTORY.SET_GOAL');
        expect(text).not.toContain('WAIST_HISTORY.CANCEL_GOAL');
        component['close']();
        expect(close).toHaveBeenCalledOnce();
    });
});

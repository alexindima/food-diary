import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../../shared/development/development-tools';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WeightHistoryEntryDialogComponent } from './weight-history-entry-dialog';

const FIXTURE_INITIAL_VERSION = 7;
const FIXTURE_TARGET_MEASUREMENT = 75;
const createFacade = (): {
    formModel: WritableSignal<{ date: string; weight: string }>;
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
        formModel: model,
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

function setup(initialError: string | null = null): {
    fixture: ComponentFixture<WeightHistoryEntryDialogComponent>;
    component: WeightHistoryEntryDialogComponent;
    facade: ReturnType<typeof createFacade>;
    close: Mock;
    attach: Mock<(screen: DevelopmentScreen) => () => void>;
} {
    const close = vi.fn();
    const attach = vi.fn((_screen: DevelopmentScreen) => vi.fn());
    TestBed.configureTestingModule({
        imports: [WeightHistoryEntryDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: DEVELOPMENT_SCREENS, useValue: { attach } },
            { provide: WeightHistoryFacade, useFactory: createFacade },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const facade = TestBed.inject(WeightHistoryFacade) as unknown as ReturnType<typeof createFacade>;
    facade.entryError.set(initialError);
    const fixture = TestBed.createComponent(WeightHistoryEntryDialogComponent);
    fixture.detectChanges();
    return { fixture, facade, component: fixture.componentInstance, close, attach };
}

describe('Weight entry WebMCP drafts', () => {
    it.each([
        ['metric', 'kg'],
        ['imperial', 'lb'],
    ] as const)('prepares an unsaved %s draft in its displayed %s unit', (system, unit) => {
        const { fixture, facade, close, attach } = setup();
        const measurements = TestBed.inject(MeasurementSystemService);
        const previousSystem = measurements.system();
        measurements.setSystem(system);
        try {
            const screen = attach.mock.calls[0][0];
            expect(screen.kind).toBe('weight-editor');
            expect(screen.read()).toMatchObject({ unit, editing: false, loading: false, error: null });
            expect(screen.isDirty?.()).toBe(false);
            screen.fillDraft?.({ date: '2026-06-21', value: 75, unit });
            expect(facade.formModel()).toEqual({ date: '2026-06-21', weight: '75' });
            expect(screen.isDirty?.()).toBe(true);
            expect(facade.entrySaveVersion()).toBe(FIXTURE_INITIAL_VERSION);
            expect(close).not.toHaveBeenCalled();
            screen.clearDraft?.();
            expect(facade.formModel()).toEqual({ date: '2026-06-21', weight: '' });
            expect(screen.isDirty?.()).toBe(false);
            screen.beforeNavigate?.();
            expect(close).toHaveBeenCalledOnce();
        } finally {
            measurements.setSystem(previousSystem);
            fixture.destroy();
        }
        expect(attach.mock.results[0].value).toHaveBeenCalledOnce();
    });

    it('preserves the owner draft while editing or saving and prevents navigation during save', () => {
        const { facade, close, attach } = setup();
        const screen = attach.mock.calls[0][0];
        const baseline = facade.formModel();
        facade.isEditing.set(true);
        expect(() => screen.fillDraft?.({ date: '2026-06-21', value: 75, unit: 'kg' })).toThrow('NEW');
        facade.isEditing.set(false);
        facade.isSaving.set(true);
        expect(() => screen.clearDraft?.()).toThrow('NEW');
        expect(() => screen.beforeNavigate?.()).toThrow('save is running');
        expect(facade.formModel()).toEqual(baseline);
        expect(facade.entrySaveVersion()).toBe(FIXTURE_INITIAL_VERSION);
        expect(close).not.toHaveBeenCalled();
    });
});
describe('Weight entry dialog lifecycle', () => {
    it('clears a previous dialog error without discarding the measurement draft', () => {
        const { fixture, close } = setup('Previous duplicate date');
        const root = fixture.nativeElement as HTMLElement;
        expect(root.textContent).not.toContain('Previous duplicate date');
        expect(root.querySelector<HTMLInputElement>('fd-ui-input input')?.value).toBe('80');
        expect(close).not.toHaveBeenCalled();
    });
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

describe('Weight entry pending dismissal', () => {
    it.each([false, true])('blocks dismissal and editing reset while saving with editing=%s', editing => {
        const { fixture, facade, component, close } = setup();
        facade.isEditing.set(editing);
        facade.isSaving.set(true);
        fixture.detectChanges();
        component['close']();
        expect(close).not.toHaveBeenCalled();
        expect(facade.cancelEdit).not.toHaveBeenCalled();
        expect(TestBed.inject(FdUiDialogRef).disableClose).toBe(true);
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('.fd-ui-dialog__close')).toBeNull();
        expect(root.querySelector('button[type="button"]:disabled')).not.toBeNull();
        facade.isSaving.set(false);
        facade.entryError.set('Retry saving');
        fixture.detectChanges();
        expect(TestBed.inject(FdUiDialogRef).disableClose).toBe(false);
        expect(root.textContent).toContain('Retry saving');
        component['close']();
        expect(close).toHaveBeenCalledOnce();
        expect(facade.cancelEdit).toHaveBeenCalledTimes(editing ? 1 : 0);
    });
});

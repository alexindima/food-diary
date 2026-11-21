import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type FieldTree, form } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { DEVELOPMENT_SCREENS, type DevelopmentScreen } from '../../../../shared/development/development-tools';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WeightHistoryEntryDialogComponent } from './weight-history-entry-dialog';

const FIXTURE_INITIAL_VERSION = 7;
const FIXTURE_TARGET_MEASUREMENT = 75;
const IMPERIAL_ENTRY_MAXIMUM = '1102,31';
const METRIC_ENTRY_MAXIMUM = 500;
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

    it('does not let a local draft fill replace the weight entered in the open dialog', () => {
        const { fixture, facade, close, attach } = setup();
        const host = fixture.nativeElement as HTMLElement;
        const input = host.querySelector<HTMLInputElement>('fd-ui-input input');
        const screen = attach.mock.calls[0][0];
        const fillDraft = screen.fillDraft;
        if (input === null || fillDraft === undefined) {
            throw new Error('Expected the rendered weight input and its registered draft action');
        }
        expect(screen.isDirty?.()).toBe(false);
        const date = facade.formModel().date;
        const unit = TestBed.inject(MeasurementSystemService).system() === 'metric' ? 'kg' : 'lb';

        input.value = '81.5';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(screen.isDirty?.()).toBe(true);

        expect(() => {
            fillDraft({ date: '2026-06-21', value: FIXTURE_TARGET_MEASUREMENT, unit });
        }).toThrow('Unsaved measurement draft exists.');
        fixture.detectChanges();
        expect(screen.read()).toMatchObject({ date, value: '81.5' });
        expect(input.value).toBe('81.5');
        expect(facade.entrySaveVersion()).toBe(FIXTURE_INITIAL_VERSION);
        expect(close).not.toHaveBeenCalled();
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

describe('Weight entry dialog with the real measurement facade', () => {
    it('checks the displayed imperial maximum before posting the canonical measurement', async () => {
        const close = vi.fn();
        TestBed.configureTestingModule({
            imports: [WeightHistoryEntryDialogComponent],
            providers: [
                provideTranslateTesting(),
                provideHttpClient(),
                provideHttpClientTesting(),
                WeightHistoryFacade,
                { provide: FdUiDialogRef, useValue: { close } },
            ],
        });
        TestBed.inject(MeasurementSystemService).setSystem('imperial');
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { WEIGHT_HISTORY: { VALUE_RANGE: 'Enter a value no greater than {{max}}.' } });
        await firstValueFrom(translate.use('en'));
        const http = TestBed.inject(HttpTestingController);
        const fixture = TestBed.createComponent(WeightHistoryEntryDialogComponent);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const input = host.querySelector<HTMLInputElement>('fd-ui-input input');
        const submit = host.querySelector<HTMLButtonElement>('button[type="submit"]');
        if (input === null || submit === null) {
            throw new Error('Expected the measurement entry form');
        }

        input.value = '1102,32';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        submit.click();
        await waitForAsyncTasksAsync();
        http.expectNone(environment.apiUrls.weights);
        fixture.detectChanges();
        expect(host.textContent).toContain('Enter a value no greater than 1102.31.');
        expect(close).not.toHaveBeenCalled();

        input.value = IMPERIAL_ENTRY_MAXIMUM;
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        submit.click();
        await waitForAsyncTasksAsync();
        const request = http.expectOne({ method: 'POST', url: environment.apiUrls.weights });
        expect(request.request.body).toMatchObject({ weightKg: METRIC_ENTRY_MAXIMUM });
        request.flush({ id: 'entry', userId: 'owner', date: '2026-06-21', weightKg: METRIC_ENTRY_MAXIMUM });
        await waitForAsyncTasksAsync();
        http.expectOne(item => item.url === `${environment.apiUrls.weights}/page-summary`).flush({
            entries: [],
            summary: [],
            goal: {},
            goalHistory: [],
        });
        fixture.detectChanges();
        expect(close).toHaveBeenCalledOnce();
        http.verify();
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

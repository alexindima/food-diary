import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { type UnsavedChangesHandler, UnsavedChangesService } from '../services/unsaved-changes.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { unsavedChangesGuard } from './unsaved-changes.guard';

describe('unsavedChangesGuard', () => {
    it.each(['local', 'logout'])('leaves a %s-ended owner without saving and restores ordinary guest confirmation', async reason => {
        const handler = createHandler({ hasChanges: true });
        setupGuard(handler);
        const openDialog = vi.spyOn(TestBed.inject(FdUiDialogService), 'open');
        const sessions = TestBed.inject(SessionEventsService);
        if (reason === 'local') {
            sessions.notifyLocalSessionInvalidated();
        } else {
            sessions.notifySessionEnded();
        }

        await sessions.runSessionEndNavigationAsync(async () => {
            expect(await runGuard()).toBe(true);
        });
        expect(handler.save).not.toHaveBeenCalled();
        expect(handler.discard).not.toHaveBeenCalled();
        expect(openDialog).not.toHaveBeenCalled();
        expect(await runGuard()).toBe(false);
        expect(openDialog).toHaveBeenCalledOnce();
        expect(handler.save).not.toHaveBeenCalled();
    });
    it('should allow navigation when there is no handler', async () => {
        setupGuard(null);

        const result = await runGuard();

        expect(result).toBe(true);
    });

    it('should allow navigation when handler has no changes', async () => {
        setupGuard(createHandler({ hasChanges: false }));

        const result = await runGuard();

        expect(result).toBe(true);
    });

    it('should save changes when dialog returns save', async () => {
        const handler = createHandler({ hasChanges: true });
        setupGuard(handler, 'save');

        const result = await runGuard();

        expect(result).toBe(true);
        expect(handler.save).toHaveBeenCalled();
    });

    it('should block navigation when save returns false', async () => {
        const handler = createHandler({ hasChanges: true, saveResult: false });
        setupGuard(handler, 'save');

        const result = await runGuard();

        expect(result).toBe(false);
        expect(handler.save).toHaveBeenCalled();
    });

    it.each([
        { outcome: 'false', saveResult: false, canNavigate: false },
        { outcome: 'true', saveResult: true, canNavigate: true },
        { outcome: 'void', saveResult: undefined, canNavigate: true },
    ])('allows navigation according to a synchronous save returning $outcome', async ({ saveResult, canNavigate }) => {
        const handler: UnsavedChangesHandler = {
            hasChanges: () => true,
            save: vi.fn(() => saveResult),
            discard: vi.fn(),
        };
        setupGuard(handler, 'save');

        expect(await runGuard()).toBe(canNavigate);
        expect(handler.save).toHaveBeenCalledOnce();
        expect(handler.discard).not.toHaveBeenCalled();
    });

    it('should discard changes when dialog returns discard', async () => {
        const handler = createHandler({ hasChanges: true });
        setupGuard(handler, 'discard');

        const result = await runGuard();

        expect(result).toBe(true);
        expect(handler.discard).toHaveBeenCalled();
    });

    it('should block navigation when dialog is cancelled', async () => {
        setupGuard(createHandler({ hasChanges: true }));

        const result = await runGuard();

        expect(result).toBe(false);
    });
});

function setupGuard(handler: UnsavedChangesHandler | null, dialogResult?: 'save' | 'discard'): void {
    TestBed.configureTestingModule({
        providers: [
            {
                provide: FdUiDialogService,
                useValue: {
                    open: vi.fn().mockReturnValue({
                        afterClosed: () => of(dialogResult),
                    }),
                },
            },
        ],
    });
    if (handler !== null) {
        TestBed.inject(UnsavedChangesService).register(handler);
    }
}

function createHandler(options: { hasChanges: boolean; saveResult?: boolean }): UnsavedChangesHandler {
    return {
        hasChanges: vi.fn().mockReturnValue(options.hasChanges),
        save: vi.fn<UnsavedChangesHandler['save']>().mockResolvedValue(options.saveResult ?? true),
        discard: vi.fn(),
    };
}

async function resolveGuardResultAsync(value: ReturnType<typeof unsavedChangesGuard>): Promise<unknown> {
    return isObservable(value) ? firstValueFrom(value) : value;
}

async function runGuard(): Promise<unknown> {
    const [component, currentRoute, currentState, nextState] = createGuardArguments();
    return TestBed.runInInjectionContext(async () =>
        resolveGuardResultAsync(unsavedChangesGuard(component, currentRoute, currentState, nextState)),
    );
}

function createGuardArguments(): Parameters<typeof unsavedChangesGuard> {
    const currentRouteStub = {};
    const currentStateStub = {};
    const nextStateStub = {};

    return [{}, currentRouteStub as ActivatedRouteSnapshot, currentStateStub as RouterStateSnapshot, nextStateStub as RouterStateSnapshot];
}

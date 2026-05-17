import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { type UnsavedChangesHandler, UnsavedChangesService } from '../services/unsaved-changes.service';
import { unsavedChangesGuard } from './unsaved-changes.guard';

describe('unsavedChangesGuard', () => {
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

    it('should discard changes when dialog returns discard', async () => {
        const handler = createHandler({ hasChanges: true });
        setupGuard(handler, 'discard');

        const result = await runGuard();

        expect(result).toBe(true);
        expect(handler.discard).toHaveBeenCalled();
    });

    it('should block navigation when dialog is cancelled', async () => {
        setupGuard(createHandler({ hasChanges: true }), undefined);

        const result = await runGuard();

        expect(result).toBe(false);
    });
});

function setupGuard(handler: UnsavedChangesHandler | null, dialogResult?: 'save' | 'discard'): void {
    TestBed.configureTestingModule({
        providers: [
            {
                provide: UnsavedChangesService,
                useValue: {
                    getHandler: vi.fn().mockReturnValue(handler),
                },
            },
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
}

function createHandler(options: { hasChanges: boolean }): UnsavedChangesHandler {
    return {
        hasChanges: vi.fn().mockReturnValue(options.hasChanges),
        save: vi.fn().mockResolvedValue(undefined),
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

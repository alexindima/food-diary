import { Component, DestroyRef, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it, vi } from 'vitest';

import { SessionEventsService } from '../auth/session-events.service';
import { editorUnsavedChangesGuard } from './editor-unsaved-changes.guard';

const confirmLeave = vi.fn<() => Promise<boolean>>().mockResolvedValue(false);
const disposePrivateOwner = vi.fn();

@Component({ template: 'Private draft' })
class PrivateEditor {
    public constructor() {
        inject(DestroyRef).onDestroy(disposePrivateOwner);
    }

    public async canLeaveAsync(): Promise<boolean> {
        return confirmLeave();
    }
}

@Component({ template: 'Sign in' })
class LoginPage {}

describe('editorUnsavedChangesGuard detached owner', () => {
    it('allows ordinary navigation when the routed editor has already been destroyed', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([])] });
        const state = TestBed.inject(Router).routerState.snapshot;
        expect(TestBed.inject(SessionEventsService).isSessionEndNavigation).toBe(false);

        const result = TestBed.runInInjectionContext(async () => editorUnsavedChangesGuard(null, state.root, state, state));

        expect(await result).toBe(true);
    });
});

describe('editorUnsavedChangesGuard session invalidation', () => {
    it.each(['local', 'logout'])('destroys the private route owner during %s departure despite a dirty draft', async reason => {
        confirmLeave.mockClear();
        disposePrivateOwner.mockClear();
        TestBed.configureTestingModule({
            providers: [
                provideRouter([
                    { path: 'editor', component: PrivateEditor, canDeactivate: [editorUnsavedChangesGuard] },
                    { path: 'login', component: LoginPage },
                ]),
            ],
        });
        const harness = await RouterTestingHarness.create('/editor');

        await harness.navigateByUrl('/login');
        expect(harness.routeNativeElement?.textContent).toContain('Private draft');
        expect(disposePrivateOwner).not.toHaveBeenCalled();
        confirmLeave.mockClear();

        const sessions = TestBed.inject(SessionEventsService);
        if (reason === 'local') {
            sessions.notifyLocalSessionInvalidated();
        } else {
            sessions.notifySessionEnded();
        }
        await sessions.runSessionEndNavigationAsync(async () => {
            await harness.navigateByUrl('/login', LoginPage);
        });

        expect(confirmLeave).not.toHaveBeenCalled();
        expect(disposePrivateOwner).toHaveBeenCalledOnce();
        expect(harness.routeNativeElement?.textContent).toContain('Sign in');
        await harness.navigateByUrl('/editor');
        await harness.navigateByUrl('/login');
        expect(harness.routeNativeElement?.textContent).toContain('Private draft');
    });
});

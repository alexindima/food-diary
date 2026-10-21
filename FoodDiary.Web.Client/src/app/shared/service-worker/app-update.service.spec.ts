import { DOCUMENT } from '@angular/common';
import { ApplicationRef, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SwUpdate, type UnrecoverableStateEvent, type VersionEvent } from '@angular/service-worker';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { AppUpdateService } from './app-update.service';
import { ServiceWorkerDiagnosticsService } from './service-worker-diagnostics.service';

const HOUR_MS = 3_600_000;
let events: Subject<VersionEvent>;
let unrecoverable: Subject<UnrecoverableStateEvent>;
let stable: Subject<boolean>;
let view: EventTarget & { navigator: { onLine: boolean }; location: { reload: ReturnType<typeof vi.fn> } };
let documentRef: EventTarget & { visibilityState: string };
let checkForUpdate: ReturnType<typeof vi.fn>;
let report: ReturnType<typeof vi.fn>;

beforeEach(() => {
    vi.useFakeTimers();
    events = new Subject<VersionEvent>();
    unrecoverable = new Subject<UnrecoverableStateEvent>();
    report = vi.fn();
    stable = new Subject<boolean>();
    checkForUpdate = vi.fn().mockResolvedValue(false);
    view = Object.assign(new EventTarget(), {
        navigator: { onLine: true },
        location: { reload: vi.fn() },
        setInterval: (callback: () => void, ms: number) => setInterval(callback, ms),
        clearInterval: (id: ReturnType<typeof setInterval>) => {
            clearInterval(id);
        },
    });
    documentRef = Object.assign(new EventTarget(), { defaultView: view, visibilityState: 'visible' });
    TestBed.configureTestingModule({
        providers: [
            AppUpdateService,
            { provide: DOCUMENT, useValue: documentRef },
            { provide: PLATFORM_ID, useValue: 'browser' },
            { provide: ApplicationRef, useValue: { isStable: stable } },
            { provide: SwUpdate, useValue: { isEnabled: true, versionUpdates: events, unrecoverable, checkForUpdate } },
            { provide: ServiceWorkerDiagnosticsService, useValue: { report } },
        ],
    });
});

afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
});

function ready(hash = 'new-version'): void {
    events.next({ type: 'VERSION_READY', currentVersion: { hash: 'old-version' }, latestVersion: { hash } });
}

function recover(): void {
    unrecoverable.next({ type: 'UNRECOVERABLE_STATE', reason: 'A lazy chunk is no longer available.' });
}

describe('AppUpdateService version availability', () => {
    it('keeps recovery actionable even after an ordinary update was dismissed', () => {
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        ready();
        service.dismiss();
        recover();
        service.dismiss();
        expect(service.recoveryRequired()).toBe(true);
        expect(service.updateAvailable()).toBe(true);
        expect(service.hasPendingUpdate()).toBe(true);
        expect(view.location.reload).not.toHaveBeenCalled();
        expect(report).toHaveBeenCalledWith('recovery');
    });

    it('reports a failed download without treating it as ready or discarding an earlier ready version', () => {
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        const failure = { type: 'VERSION_INSTALLATION_FAILED', version: { hash: 'failed-version' }, error: 'private URL' } as const;
        events.next(failure);
        expect(report).toHaveBeenCalledWith('installation', 'failed-version');
        expect(service.updateAvailable()).toBe(false);
        ready();
        events.next(failure);
        expect(service.updateAvailable()).toBe(true);
        expect(view.location.reload).not.toHaveBeenCalled();
    });

    it('notifies only for downloaded versions and dismisses only the current version', () => {
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        events.next({ type: 'VERSION_DETECTED', version: { hash: 'new-version' } });
        expect(service.updateAvailable()).toBe(false);
        ready();
        expect(service.updateAvailable()).toBe(true);
        expect(view.location.reload).not.toHaveBeenCalled();
        service.dismiss();
        ready();
        expect(service.updateAvailable()).toBe(false);
        expect(service.hasPendingUpdate()).toBe(true);
        ready('newer-version');
        expect(service.updateAvailable()).toBe(true);
    });
});

describe('AppUpdateService checks', () => {
    it('checks after stability, throttles resume checks and skips hidden/offline checks', async () => {
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        service.initialize();
        expect(checkForUpdate).not.toHaveBeenCalled();
        stable.next(true);
        await vi.advanceTimersByTimeAsync(0);
        expect(checkForUpdate).toHaveBeenCalledOnce();
        documentRef.dispatchEvent(new Event('visibilitychange'));
        expect(checkForUpdate).toHaveBeenCalledOnce();
        documentRef.visibilityState = 'hidden';
        await vi.advanceTimersByTimeAsync(HOUR_MS);
        expect(checkForUpdate).toHaveBeenCalledOnce();
        documentRef.visibilityState = 'visible';
        view.navigator.onLine = false;
        documentRef.dispatchEvent(new Event('visibilitychange'));
        expect(checkForUpdate).toHaveBeenCalledOnce();
        view.navigator.onLine = true;
        view.dispatchEvent(new Event('online'));
        await vi.advanceTimersByTimeAsync(0);
        expect(checkForUpdate).toHaveBeenCalledTimes(2);
    });

    it('continues after a failed update check and cleans up timers/listeners', async () => {
        checkForUpdate.mockRejectedValueOnce(new Error('offline'));
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        stable.next(true);
        await vi.advanceTimersByTimeAsync(HOUR_MS);
        expect(checkForUpdate).toHaveBeenCalledTimes(2);
        expect(report).toHaveBeenCalledWith('check');
        TestBed.resetTestingModule();
        await vi.advanceTimersByTimeAsync(HOUR_MS);
        view.dispatchEvent(new Event('online'));
        expect(checkForUpdate).toHaveBeenCalledTimes(2);
    });
});

describe('AppUpdateService departure protection', () => {
    it.each([ready, recover])('keeps drafts intact when an editor cancels, then reloads after explicit approval (%#)', async trigger => {
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        trigger();
        await service.reloadAsync(vi.fn<() => Promise<boolean>>().mockResolvedValue(false));
        expect(view.location.reload).not.toHaveBeenCalled();
        expect(service.updateAvailable()).toBe(true);
        await service.reloadAsync(vi.fn<() => Promise<boolean>>().mockResolvedValue(true));
        expect(view.location.reload).toHaveBeenCalledOnce();
    });

    it.each([ready, recover])('blocks reloading while the registered form has unsaved changes (%#)', async trigger => {
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        trigger();
        const hasChanges = vi.fn().mockReturnValue(true);
        TestBed.inject(UnsavedChangesService).register({ hasChanges, save: () => false, discard: () => {} });
        const confirm = vi.fn().mockResolvedValue(true);
        await service.reloadAsync(confirm);
        expect(confirm).not.toHaveBeenCalled();
        expect(service.needsSave()).toBe(true);
        expect(view.location.reload).not.toHaveBeenCalled();
        hasChanges.mockReturnValue(false);
        await service.reloadAsync(confirm);
        expect(view.location.reload).toHaveBeenCalledOnce();
    });

    it.each([ready, recover])('rechecks for edits made during the departure decision (%#)', async trigger => {
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        trigger();
        await service.reloadAsync(
            vi.fn<() => Promise<boolean>>().mockImplementation(async () => {
                await waitForAsyncTasksAsync();
                TestBed.inject(UnsavedChangesService).register({ hasChanges: () => true, save: () => false, discard: () => {} });
                return true;
            }),
        );
        expect(view.location.reload).not.toHaveBeenCalled();
    });

    it.each(['disabled', 'server'])('does nothing when service workers are %s', context => {
        if (context === 'disabled') {
            TestBed.overrideProvider(SwUpdate, { useValue: { isEnabled: false } });
        }
        if (context === 'server') {
            TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        }
        const service = TestBed.inject(AppUpdateService);
        service.initialize();
        stable.next(true);
        ready();
        expect(checkForUpdate).not.toHaveBeenCalled();
        expect(service.updateAvailable()).toBe(false);
    });
});

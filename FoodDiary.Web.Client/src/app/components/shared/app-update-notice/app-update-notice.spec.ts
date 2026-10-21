import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { SwUpdate, type UnrecoverableStateEvent, type VersionEvent } from '@angular/service-worker';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { UnsavedChangesService } from '../../../services/unsaved-changes.service';
import { AppUpdateService } from '../../../shared/service-worker/app-update.service';
import { ServiceWorkerDiagnosticsService } from '../../../shared/service-worker/service-worker-diagnostics.service';
import { AppUpdateNoticeComponent } from './app-update-notice';

let fixture: ComponentFixture<AppUpdateNoticeComponent>;
let root: HTMLElement;
let unrecoverable: Subject<UnrecoverableStateEvent>;
let events: Subject<VersionEvent>;
let reload: ReturnType<typeof vi.fn<() => void>>;
let confirm: ReturnType<typeof vi.fn<() => Promise<boolean>>>;

beforeEach(async () => {
    unrecoverable = new Subject<UnrecoverableStateEvent>();
    events = new Subject<VersionEvent>();
    reload = vi.fn();
    confirm = vi.fn<() => Promise<boolean>>().mockResolvedValue(true);
    await TestBed.configureTestingModule({
        imports: [AppUpdateNoticeComponent],
        providers: [
            provideTranslateTesting(),
            { provide: PLATFORM_ID, useValue: 'browser' },
            {
                provide: SwUpdate,
                useValue: { isEnabled: true, versionUpdates: events, unrecoverable, checkForUpdate: vi.fn().mockResolvedValue(false) },
            },
            { provide: ServiceWorkerDiagnosticsService, useValue: { report: vi.fn() } },
        ],
    }).compileComponents();
    const documentRef = TestBed.inject(DOCUMENT);
    const view = documentRef.defaultView;
    if (view === null) {
        throw new Error('Expected a browser window');
    }
    const browserView = new Proxy(view, {
        get: (target, property): unknown => {
            const value: unknown = property === 'location' ? { reload } : Reflect.get(target, property, target);
            return value;
        },
    });
    vi.spyOn(documentRef, 'defaultView', 'get').mockReturnValue(browserView);
    TestBed.inject(AppUpdateService).initialize();
    fixture = TestBed.createComponent(AppUpdateNoticeComponent);
    fixture.componentRef.setInput('visible', false);
    fixture.componentRef.setInput('confirmReload', confirm);
    fixture.detectChanges();
    const element: unknown = fixture.nativeElement;
    if (!(element instanceof HTMLElement)) {
        throw new Error('Expected rendered update notice');
    }
    root = element;
});

afterEach(() => {
    vi.restoreAllMocks();
});

function fail(): void {
    unrecoverable.next({ type: 'UNRECOVERABLE_STATE', reason: 'Private technical reason' });
    fixture.detectChanges();
}

function reloadButton(): HTMLButtonElement {
    const button = root.querySelector('button');
    if (button === null) {
        throw new Error('Expected explicit reload button');
    }
    return button;
}

describe('App update and recovery notice', () => {
    it('shows recovery on public routes and keeps it visible with a single explicit reload action', () => {
        fail();
        expect(root.textContent).toContain('PWA.RECOVERY.TITLE');
        expect(root.textContent).toContain('PWA.RECOVERY.DESCRIPTION');
        expect(root.textContent).not.toContain('Private technical reason');
        expect(root.querySelectorAll('button')).toHaveLength(1);
        expect(reload).not.toHaveBeenCalled();
        TestBed.inject(AppUpdateService).dismiss();
        fixture.detectChanges();
        expect(root.querySelector('button')).not.toBeNull();
    });

    it('preserves a registered draft until it is saved and the user clicks reload again', async () => {
        const hasChanges = vi.fn().mockReturnValue(true);
        TestBed.inject(UnsavedChangesService).register({ hasChanges, save: () => false, discard: () => {} });
        fail();
        reloadButton().click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(root.textContent).toContain('PWA.UPDATE.SAVE_FIRST');
        expect(reload).not.toHaveBeenCalled();
        expect(confirm).not.toHaveBeenCalled();
        hasChanges.mockReturnValue(false);
        reloadButton().click();
        await fixture.whenStable();
        expect(confirm).toHaveBeenCalledOnce();
        expect(reload).toHaveBeenCalledOnce();
    });

    it('honors an editor cancellation through the rendered recovery action', async () => {
        confirm.mockResolvedValue(false);
        fail();
        reloadButton().click();
        await fixture.whenStable();
        expect(reload).not.toHaveBeenCalled();
        expect(root.textContent).toContain('PWA.RECOVERY.TITLE');
    });

    it('keeps ordinary update notices within the authenticated shell and allows Later', () => {
        events.next({ type: 'VERSION_READY', currentVersion: { hash: 'old' }, latestVersion: { hash: 'new' } });
        fixture.detectChanges();
        expect(root.querySelector('button')).toBeNull();
        fixture.componentRef.setInput('visible', true);
        fixture.detectChanges();
        expect(root.textContent).toContain('PWA.UPDATE.TITLE');
        const buttons = root.querySelectorAll('button');
        expect(buttons).toHaveLength(2);
        buttons[1].click();
        fixture.detectChanges();
        expect(root.querySelector('button')).toBeNull();
    });
});

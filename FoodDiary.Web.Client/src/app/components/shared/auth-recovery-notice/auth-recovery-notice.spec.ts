import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { type Observable, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { AuthRecoveryStateService } from '../../../shared/auth/auth-recovery-state.service';
import { AuthRecoveryNoticeComponent } from './auth-recovery-notice';

let fixture: ComponentFixture<AuthRecoveryNoticeComponent>;
let recovery: AuthRecoveryStateService;
let auth: {
    refreshSession: Mock<() => Observable<boolean>>;
    retryPendingLogoutAsync: Mock<() => Promise<void>>;
    isAuthenticated: Mock<() => boolean>;
};
let navigation: { navigateToReturnUrlAsync: ReturnType<typeof vi.fn>; navigateToAuthAsync: ReturnType<typeof vi.fn> };

beforeEach(async () => {
    localStorage.clear();
    sessionStorage.clear();
    auth = {
        refreshSession: vi.fn().mockReturnValue(of(false)),
        retryPendingLogoutAsync: vi.fn().mockResolvedValue(undefined),
        isAuthenticated: vi.fn().mockReturnValue(false),
    };
    navigation = {
        navigateToReturnUrlAsync: vi.fn().mockResolvedValue(undefined),
        navigateToAuthAsync: vi.fn().mockResolvedValue(undefined),
    };
    await TestBed.configureTestingModule({
        imports: [AuthRecoveryNoticeComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AuthService, useValue: auth },
            { provide: NavigationService, useValue: navigation },
        ],
    }).compileComponents();
    recovery = TestBed.inject(AuthRecoveryStateService);
    fixture = TestBed.createComponent(AuthRecoveryNoticeComponent);
});

afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
});

function root(): HTMLElement {
    const element: unknown = fixture.nativeElement;
    if (!(element instanceof HTMLElement)) {
        throw new Error('Expected a rendered recovery notice');
    }
    return element;
}

function retryButton(): HTMLButtonElement {
    const button = root().querySelector('button');
    if (button === null) {
        throw new Error('Expected an explicit retry action');
    }
    return button;
}

describe('authentication recovery notice', () => {
    it('does not render a warning for a known authentication state', () => {
        fixture.detectChanges();
        expect(root().querySelector('[role="status"]')).toBeNull();
    });

    it('explains temporary restoration failure and returns to the intended route after a successful explicit retry', async () => {
        recovery.restorationFailed(null);
        recovery.returnUrl = '/products';
        auth.refreshSession.mockImplementation(() => {
            recovery.restorationSucceeded();
            return of(true);
        });
        fixture.detectChanges();
        expect(root().textContent).toContain('SESSION_RECOVERY.RESTORE_UNAVAILABLE');
        retryButton().click();
        await fixture.whenStable();
        expect(auth.refreshSession).toHaveBeenCalledOnce();
        expect(navigation.navigateToReturnUrlAsync).toHaveBeenCalledWith('/products');
        fixture.detectChanges();
        expect(root().querySelector('[role="status"]')).toBeNull();
    });

    it('retries pending sign-out rather than restoring authenticated access', async () => {
        recovery.beginLogout({ sessionId: 'public-session', contextId: null, telegram: false });
        fixture.detectChanges();
        expect(root().textContent).toContain('SESSION_RECOVERY.LOGOUT_PENDING');
        retryButton().click();
        await fixture.whenStable();
        expect(auth.retryPendingLogoutAsync).toHaveBeenCalledOnce();
        expect(auth.refreshSession).not.toHaveBeenCalled();
    });

    it('does not submit another request while recovery is busy', () => {
        recovery.restorationFailed(null);
        recovery.busy.set(true);
        fixture.detectChanges();
        expect(root().querySelector('button')).toBeNull();
        expect(root().querySelector('[aria-busy="true"]')).not.toBeNull();
    });

    it('returns to sign-in when a pending logout retry is confirmed', async () => {
        const pending = { sessionId: 'previous-session', contextId: null, telegram: false };
        recovery.beginLogout(pending);
        recovery.returnUrl = '/meals';
        fixture.detectChanges();
        retryButton().click();
        recovery.confirmLogout(pending);
        await fixture.whenStable();
        expect(navigation.navigateToAuthAsync, 'Confirmed logout retry must reach sign-in').toHaveBeenCalledWith('login', '/meals');
        expect(auth.refreshSession).not.toHaveBeenCalled();
    });

    it('does not navigate over a fresh login completed while logout retry was pending', async () => {
        const pending = { sessionId: 'previous-session', contextId: null, telegram: false };
        recovery.beginLogout(pending);
        fixture.detectChanges();
        retryButton().click();
        recovery.confirmLogout(pending);
        auth.isAuthenticated.mockReturnValue(true);
        await fixture.whenStable();
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
    });
});

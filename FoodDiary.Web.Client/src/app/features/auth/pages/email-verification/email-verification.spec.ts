import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { TelegramDiaryPreview } from '../../../../shared/auth/telegram-diary.data';
import { EmailVerificationComponent } from './email-verification';
import { TelegramDiaryTransferComponent } from './telegram-diary-transfer';

let authServiceMock: {
    onLogoutAsync: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
    getUserId: ReturnType<typeof vi.fn>;
    verifyEmail: ReturnType<typeof vi.fn>;
    verifyTelegramBackupEmail: ReturnType<typeof vi.fn>;
};
let navigationServiceMock: {
    navigateToAuthAsync: ReturnType<typeof vi.fn>;
    navigateToHomeAsync: ReturnType<typeof vi.fn>;
    navigateToEmailVerificationPendingAsync: ReturnType<typeof vi.fn>;
};
let navigateByUrl: ReturnType<typeof vi.fn>;

beforeEach(() => {
    navigateByUrl = vi.fn().mockResolvedValue(true);
    authServiceMock = {
        onLogoutAsync: vi.fn().mockResolvedValue(void 0),
        isAuthenticated: vi.fn().mockReturnValue(true),
        getUserId: vi.fn().mockReturnValue('user-1'),
        verifyEmail: vi.fn().mockReturnValue(of(true)),
        verifyTelegramBackupEmail: vi.fn().mockReturnValue(of(null)),
    };
    navigationServiceMock = {
        navigateToAuthAsync: vi.fn().mockResolvedValue(void 0),
        navigateToHomeAsync: vi.fn().mockResolvedValue(void 0),
        navigateToEmailVerificationPendingAsync: vi.fn().mockResolvedValue(void 0),
    };
});

describe('EmailVerificationComponent backup email', () => {
    it('requires a fresh sign-in after backup email confirmation revokes sessions', () => {
        const component = createComponent({ userId: 'user-1', token: 'telegram-backup.ticket' });
        expect(component['state']()).toBe('success');
        component['onContinue']();
        expect(authServiceMock.onLogoutAsync).toHaveBeenCalledWith(true);
        expect(navigationServiceMock.navigateToHomeAsync).not.toHaveBeenCalled();
    });

    it('explains how to recover an expired or already-used backup link', () => {
        authServiceMock.verifyTelegramBackupEmail.mockReturnValue(throwError(() => new Error('expired')));
        const component = createComponent({ userId: 'user-1', token: 'telegram-backup.ticket' });
        expect(component['errorMessage']()).toBe('USER_MANAGE.BACKUP_EMAIL_VERIFY_ERROR');
    });

    it('discards the transfer ticket when the user keeps the diaries separate', () => {
        authServiceMock.verifyTelegramBackupEmail.mockReturnValue(of('browser-bound-ticket'));
        const component = createComponent({ userId: 'user-1', token: 'telegram-backup.ticket' });
        expect(component['transferTicket']()).toBe('browser-bound-ticket');
        expect(navigateByUrl).toHaveBeenCalledWith('/verify-email', { replaceUrl: true });
        component['onKeepDiariesSeparate']();
        expect(component['transferTicket']()).toBeNull();
        expect(component['state']()).toBe('cancelled');
    });
});

describe('EmailVerificationComponent', () => {
    it('should verify email when token is present', () => {
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        expect(authServiceMock.verifyEmail).toHaveBeenCalledWith('user-1', 'token-1');
        expect(component['state']()).toBe('success');
        expect(component['isBusy']()).toBe(false);
    });

    it('provides a fresh-link action for an invalid token and recovers through the signed-in account', () => {
        authServiceMock.verifyEmail.mockReturnValue(throwError(() => ({ error: { error: 'Authentication.InvalidToken' } })));
        const component = createComponent({ userId: 'user-1', token: 'expired' });
        expect(component['state']()).toBe('invalid');
        expect(component['errorMessage']()).toBe('AUTH.VERIFY.ERROR_INVALID');
        component['onRequestNewLink']();
        expect(navigationServiceMock.navigateToEmailVerificationPendingAsync).toHaveBeenCalledOnce();
    });

    it('requires sign-in when the invalid link belongs to another account', () => {
        authServiceMock.verifyEmail.mockReturnValue(throwError(() => ({ error: { error: 'Authentication.InvalidToken' } })));
        const component = createComponent({ userId: 'another-user', token: 'expired' });
        component['onRequestNewLink']();
        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login');
        expect(navigationServiceMock.navigateToEmailVerificationPendingAsync).not.toHaveBeenCalled();
    });

    it('should set invalid error when token is missing', () => {
        const component = createComponent({});

        expect(authServiceMock.verifyEmail).not.toHaveBeenCalled();
        expect(component['state']()).toBe('invalid');
        expect(component['errorMessage']()).toBe('AUTH.VERIFY.ERROR_INVALID');
    });

    it('should navigate to login instead of retrying when token is missing', () => {
        const component = createComponent({});

        component['onBackToLogin']();

        expect(authServiceMock.verifyEmail).not.toHaveBeenCalled();
        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login');
    });

    it('should render an h1 and a login action for an invalid link', () => {
        TestBed.configureTestingModule({
            imports: [EmailVerificationComponent],
            providers: [
                provideTranslateTesting(),
                { provide: AuthService, useValue: authServiceMock },
                { provide: NavigationService, useValue: navigationServiceMock },
                {
                    provide: ActivatedRoute,
                    useValue: {
                        snapshot: {
                            queryParamMap: convertToParamMap({}),
                        },
                    },
                },
            ],
        });

        const fixture = TestBed.createComponent(EmailVerificationComponent);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelector('h1')?.textContent).toContain('AUTH.VERIFY.TITLE');
        expect(root.querySelector('button')?.textContent).toContain('AUTH.VERIFY.REQUEST_NEW_LINK');
        expect(root.textContent).not.toContain('AUTH.VERIFY.RETRY');
    });

    it('should allow retry with the resolved token', () => {
        authServiceMock.verifyEmail.mockReturnValueOnce(throwError(() => new Error('fail'))).mockReturnValueOnce(of(true));
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        expect(component['state']()).toBe('error');

        component['onRetry']();

        expect(authServiceMock.verifyEmail).toHaveBeenCalledTimes(2);
        expect(component['state']()).toBe('success');
    });

    it('should navigate home on continue when authenticated', () => {
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        component['onContinue']();

        expect(navigationServiceMock.navigateToHomeAsync).toHaveBeenCalled();
    });

    it('should navigate to login on continue when unauthenticated', () => {
        authServiceMock.isAuthenticated.mockReturnValue(false);
        const component = createComponent({ userId: 'user-1', token: 'token-1' });

        component['onContinue']();

        expect(navigationServiceMock.navigateToAuthAsync).toHaveBeenCalledWith('login');
    });
});

describe('Telegram diary transfer review', () => {
    it('explains why email goals stay and confirms directly when they are the only option', () => {
        const { root, confirm, transfer } = openTransferReview(false);

        expect(root.textContent).toContain('AUTH.TELEGRAM_DIARY.TRANSFER_ITEMS');
        expect(root.querySelector('ul')?.textContent).toContain('AUTH.TELEGRAM_DIARY.MEALS');
        expect(root.textContent).toContain('AUTH.TELEGRAM_DIARY.KEEP_EMAIL_GOALS');
        expect(root.querySelector('input[type="radio"]')).toBeNull();
        expect(root.querySelector('[role="radiogroup"]')?.closest('[hidden]')).not.toBeNull();
        expect(confirm.disabled).toBe(false);
        expect(transfer).toHaveBeenCalledTimes(1);

        confirm.click();

        expect(transfer).toHaveBeenLastCalledWith('transfer-ticket', false);
    });

    it('asks for a choice when both diaries can supply goals', () => {
        const { root, confirm, transfer, fixture } = openTransferReview(true);
        const radios = root.querySelectorAll<HTMLInputElement>('input[type="radio"]');

        expect(radios).toHaveLength(2);
        expect(confirm.disabled).toBe(true);
        radios[1].click();
        fixture.detectChanges();
        expect(confirm.disabled).toBe(false);
        confirm.click();

        expect(transfer).toHaveBeenLastCalledWith('transfer-ticket', true);
    });
});

function openTransferReview(canUseTelegramGoals: boolean): {
    root: HTMLElement;
    confirm: HTMLButtonElement;
    transfer: ReturnType<typeof vi.fn>;
    fixture: ComponentFixture<TelegramDiaryTransferComponent>;
} {
    const goals = { calories: 2000, protein: 150, fat: 65, carbs: 200, fiber: 28, water: 2000, steps: null, weight: null, waist: null };
    const preview: TelegramDiaryPreview = {
        counts: { meals: 1, recipes: 0, products: 0, waterEntries: 0, measurements: 0 },
        emailGoals: goals,
        telegramGoals: goals,
        canUseTelegramGoals,
    };
    const transfer = vi.fn().mockReturnValue(of(preview));
    TestBed.configureTestingModule({
        imports: [TelegramDiaryTransferComponent],
        providers: [provideTranslateTesting(), { provide: AuthService, useValue: { transferTelegramDiary: transfer } }],
    });
    const fixture = TestBed.createComponent(TelegramDiaryTransferComponent);
    fixture.componentRef.setInput('ticket', 'transfer-ticket');
    fixture.componentInstance['loadPreview']();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const confirm = root.querySelector<HTMLButtonElement>('fd-ui-button button');
    if (confirm === null) {
        throw new Error('Expected a transfer confirmation button.');
    }
    return { root, confirm, transfer, fixture };
}

function createComponent(queryParams: Record<string, string>): EmailVerificationComponent {
    TestBed.configureTestingModule({
        imports: [EmailVerificationComponent],
        providers: [
            { provide: AuthService, useValue: authServiceMock },
            { provide: NavigationService, useValue: navigationServiceMock },
            { provide: Router, useValue: { navigateByUrl } },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        queryParamMap: convertToParamMap(queryParams),
                    },
                },
            },
        ],
    }).overrideComponent(EmailVerificationComponent, {
        set: { template: '' },
    });

    const fixture = TestBed.createComponent(EmailVerificationComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
}

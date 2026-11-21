import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { TelegramDiaryGoals, TelegramDiaryPreview } from '../../../../shared/auth/telegram-diary.data';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { AuthComponent } from '../../components/auth/auth';
import { AuthFlowFacade } from '../../components/auth/auth-lib/auth-flow.facade';
import { AuthFormManager } from '../../components/auth/auth-lib/auth-form.manager';
import { AuthGoogleManager } from '../../components/auth/auth-lib/auth-google.manager';
import { EmailVerificationComponent } from './email-verification';

const VERIFY_URL = '/verify-email';
const OTHER_URL = '/diary';
const SOURCE = 'telegram-backup-owner';
const ACCOUNT = 'verified-email-owner';
const PROOF = 'telegram-backup.review-fixture';
const TICKET = 'browser-bound-transfer-fixture';
const EMAIL = 'verified-account@example.test';
const PASSWORD = 'test-only-sign-in';
const NO_GOALS: TelegramDiaryGoals = {
    calories: null,
    protein: null,
    fat: null,
    carbs: null,
    fiber: null,
    water: null,
    steps: null,
    weight: null,
    waist: null,
};
const PREVIEW: TelegramDiaryPreview = {
    counts: { meals: 2, recipes: 1, products: 0, waterEntries: 0, measurements: 0 },
    emailGoals: { ...NO_GOALS, calories: 2100 },
    telegramGoals: { ...NO_GOALS, calories: 2250 },
    canUseTelegramGoals: true,
};

@Component({
    selector: 'fd-verification-diary-fixture',
    template: '<h1>Diary</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class DiaryDestinationFixture {}

@Component({
    selector: 'fd-verification-sign-in-destination-fixture',
    template: '<h1>Sign in</h1>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class SignInDestinationFixture {}

type VerificationSetup = {
    backup?: Observable<string | null>;
    standard?: Observable<void>;
    owner?: string | null;
};

type VerificationAuthMethod =
    | 'getUserId'
    | 'requiresEmailVerification'
    | 'mustChangePassword'
    | 'onLogoutAsync'
    | 'logoutForAccountSwitchAsync'
    | 'verifyEmail'
    | 'verifyTelegramBackupEmail'
    | 'transferTelegramDiary';
type VerificationMocks = {
    auth: Pick<AuthService, 'isAuthenticated'> & { [Method in VerificationAuthMethod]: Mock<AuthService[Method]> };
    navigation: {
        [Method in 'navigateToAuthAsync' | 'navigateToHomeAsync' | 'navigateToProfileAsync']: Mock<NavigationService[Method]>;
    };
    login: Mock<AuthFlowFacade['login']>;
};

function createVerificationMocks(options: VerificationSetup): VerificationMocks {
    const owner = options.owner === undefined ? ACCOUNT : options.owner;
    return {
        auth: {
            getUserId: vi.fn<AuthService['getUserId']>().mockReturnValue(owner),
            isAuthenticated: signal(owner !== null),
            requiresEmailVerification: vi.fn<AuthService['requiresEmailVerification']>().mockReturnValue(false),
            mustChangePassword: vi.fn<AuthService['mustChangePassword']>().mockReturnValue(false),
            onLogoutAsync: vi.fn<AuthService['onLogoutAsync']>().mockResolvedValue(undefined),
            logoutForAccountSwitchAsync: vi.fn<AuthService['logoutForAccountSwitchAsync']>().mockResolvedValue(undefined),
            verifyEmail: vi.fn<AuthService['verifyEmail']>().mockReturnValue(options.standard ?? of(undefined)),
            verifyTelegramBackupEmail: vi.fn<AuthService['verifyTelegramBackupEmail']>().mockReturnValue(options.backup ?? of(TICKET)),
            transferTelegramDiary: vi.fn<AuthService['transferTelegramDiary']>().mockReturnValue(of(PREVIEW)),
        },
        navigation: {
            navigateToAuthAsync: vi.fn<NavigationService['navigateToAuthAsync']>().mockResolvedValue(undefined),
            navigateToHomeAsync: vi.fn<NavigationService['navigateToHomeAsync']>().mockResolvedValue(undefined),
            navigateToProfileAsync: vi.fn<NavigationService['navigateToProfileAsync']>().mockResolvedValue(undefined),
        },
        login: vi.fn<AuthFlowFacade['login']>().mockReturnValue(of('success')),
    };
}

type VerificationFixture = VerificationMocks & { harness: RouterTestingHarness };

async function openVerificationAsync(options: VerificationSetup = {}): Promise<VerificationFixture> {
    const mocks = createVerificationMocks(options);
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter([
                { path: 'verify-email', component: EmailVerificationComponent },
                { path: 'diary', component: DiaryDestinationFixture },
            ]),
            { provide: AuthService, useValue: mocks.auth },
            { provide: NavigationService, useValue: mocks.navigation },
            { provide: AuthFlowFacade, useValue: { login: mocks.login } },
        ],
    });
    TestBed.overrideComponent(AuthComponent, {
        set: {
            providers: [
                AuthFormManager,
                {
                    provide: AuthGoogleManager,
                    useValue: { ready: signal(false), initializeAsync: vi.fn().mockResolvedValue(undefined), renderButton: vi.fn() },
                },
            ],
        },
    });
    const query = options.standard === undefined ? `userId=${SOURCE}&token=${PROOF}` : `user=${ACCOUNT}&token=regular-fixture`;
    const harness = await RouterTestingHarness.create(`${VERIFY_URL}?${query}`);
    if (options.standard === undefined) {
        await vi.waitFor(() => {
            expect(TestBed.inject(Router).url).toBe(VERIFY_URL);
        });
    }
    return { harness, ...mocks };
}

function verificationPage(harness: RouterTestingHarness): HTMLElement {
    const element = harness.routeNativeElement;
    if (element === null) {
        throw new Error('Expected the current verification route');
    }
    return element;
}

function verificationControl(harness: RouterTestingHarness, label: string): HTMLButtonElement {
    const control = [...verificationPage(harness).querySelectorAll('button')].find(
        button => button.closest('[hidden]') === null && button.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (control === undefined) {
        throw new Error(`Expected the verification action ${label}`);
    }
    return control;
}

function submitInlineLogin(harness: RouterTestingHarness): void {
    const root = verificationPage(harness);
    const form = root.querySelector<HTMLFormElement>('fd-auth-login-form form');
    if (form === null) {
        throw new Error('Expected the rendered inline sign-in form');
    }
    for (const [type, value] of [
        ['email', EMAIL],
        ['password', PASSWORD],
    ]) {
        const input = form.querySelector<HTMLInputElement>(`input[type="${type}"]`);
        if (input === null) {
            throw new Error(`Expected the sign-in ${type} input`);
        }
        input.value = value;
        input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    harness.detectChanges();
    form.requestSubmit();
}

async function expectVerificationTextAsync(harness: RouterTestingHarness, text: string): Promise<void> {
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(verificationPage(harness).textContent).toContain(text);
    });
}

function transferFailure(code: string): Observable<never> {
    return throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict, error: { error: code } }));
}

describe('Telegram diary transfer through the verification route', () => {
    it('removes the proof, retries a busy transfer, and confirms Telegram goals only once', async () => {
        const receipt = new Subject<string | null>();
        const { auth, harness, login } = await openVerificationAsync({ backup: receipt });
        expect(verificationPage(harness).textContent).toContain('AUTH.VERIFY.PENDING');
        expect(auth.verifyTelegramBackupEmail).toHaveBeenCalledExactlyOnceWith(SOURCE, PROOF);
        expect(auth.transferTelegramDiary).not.toHaveBeenCalled();

        receipt.next(TICKET);
        receipt.complete();
        harness.detectChanges();
        auth.transferTelegramDiary.mockReturnValueOnce(transferFailure('Authentication.TelegramDiaryOperationPending'));
        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.SIGN_IN').click();
        harness.detectChanges();
        submitInlineLogin(harness);
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.OPERATION_PENDING');
        expect(login).toHaveBeenCalledOnce();
        expect(auth.transferTelegramDiary).toHaveBeenCalledExactlyOnceWith(TICKET, undefined);

        verificationControl(harness, 'AUTH.VERIFY.RETRY').click();
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.REVIEW');
        expect(verificationPage(harness).querySelector('.transfer-records')?.textContent).toContain('AUTH.TELEGRAM_DIARY.MEALS');
        const confirm = verificationControl(harness, 'AUTH.TELEGRAM_DIARY.MERGE');
        expect(confirm.disabled).toBe(true);
        const radios = verificationPage(harness).querySelectorAll<HTMLInputElement>('input[type="radio"]');
        expect(radios).toHaveLength(2);
        radios[1].click();
        harness.detectChanges();
        expect(confirm.disabled).toBe(false);
        const confirmation = new Subject<TelegramDiaryPreview>();
        auth.transferTelegramDiary.mockReturnValueOnce(confirmation);
        confirm.click();
        expect(confirm.disabled).toBe(false);
        confirm.click();
        expect(auth.transferTelegramDiary.mock.calls).toEqual([
            [TICKET, undefined],
            [TICKET, undefined],
            [TICKET, true],
        ]);

        confirmation.next(PREVIEW);
        confirmation.complete();
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.DONE');
        expect(auth.transferTelegramDiary).toHaveBeenLastCalledWith(TICKET, true);
        verificationControl(harness, 'AUTH.VERIFY.BACK_TO_LOGIN').click();
        expect(auth.onLogoutAsync).toHaveBeenCalledExactlyOnceWith(true);
    });
});

describe('Telegram diary transfer review', () => {
    it('offers to connect an empty diary and retries a temporary confirmation error with the email goals', async () => {
        const { auth, harness } = await openVerificationAsync();
        const empty: TelegramDiaryPreview = {
            ...PREVIEW,
            counts: { meals: 0, recipes: 0, products: 0, waterEntries: 0, measurements: 0 },
            canUseTelegramGoals: false,
        };
        auth.transferTelegramDiary
            .mockReturnValueOnce(of(empty))
            .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable })))
            .mockReturnValueOnce(of(empty));

        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.SIGN_IN').click();
        harness.detectChanges();
        submitInlineLogin(harness);
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.REVIEW');
        expect(verificationPage(harness).querySelector('.transfer-records li')).toBeNull();
        expect(verificationPage(harness).querySelector('input[type="radio"]')).toBeNull();
        const connect = verificationControl(harness, 'AUTH.TELEGRAM_DIARY.CONNECT');
        expect(connect.disabled).toBe(false);

        connect.click();
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.ERROR');
        expect(verificationControl(harness, 'AUTH.TELEGRAM_DIARY.CONNECT').disabled).toBe(false);
        expect(auth.transferTelegramDiary).toHaveBeenLastCalledWith(TICKET, false);

        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.CONNECT').click();
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.DONE');
        expect(auth.transferTelegramDiary.mock.calls).toEqual([
            [TICKET, undefined],
            [TICKET, false],
            [TICKET, false],
        ]);
    });

    it('shows absent Telegram measurements as blank and keeps the real email measurements available to choose', async () => {
        const { auth, harness } = await openVerificationAsync();
        const weight = 74;
        const waist = 81;
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { AUTH: { TELEGRAM_DIARY: { GOAL_SUMMARY: 'Weight {{weight}}, waist {{waist}}' } } });
        await firstValueFrom(translate.use('en'));
        auth.transferTelegramDiary.mockReturnValueOnce(
            of({ ...PREVIEW, emailGoals: { ...NO_GOALS, weight, waist }, telegramGoals: { ...NO_GOALS, weight: 0, waist: 0 } }),
        );

        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.SIGN_IN').click();
        harness.detectChanges();
        submitInlineLogin(harness);

        await expectVerificationTextAsync(harness, `Weight ${weight}, waist ${waist}`);
        expect(verificationPage(harness).textContent).toContain('Weight –, waist –');
        const choices = verificationPage(harness).querySelectorAll<HTMLInputElement>('input[type="radio"]');
        expect(choices).toHaveLength(2);
        choices[0].click();
        harness.detectChanges();
        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.MERGE').click();
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.DONE');
        expect(auth.transferTelegramDiary).toHaveBeenLastCalledWith(TICKET, false);
    });
});

describe('Telegram diary transfer authorization errors', () => {
    it('binds a requested confirmation to the signed-in email account', async () => {
        const { auth, harness, login } = await openVerificationAsync();
        auth.transferTelegramDiary.mockReturnValueOnce(transferFailure('Authentication.ReauthenticationRequired'));
        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.SIGN_IN').click();
        harness.detectChanges();
        submitInlineLogin(harness);
        await expectVerificationTextAsync(harness, 'AUTH.CONFIRM.MERGE_HINT');
        expect(verificationPage(harness).querySelector('[role="alert"]:not([hidden])')?.textContent).toContain('AUTH.CONFIRM.MERGE_HINT');

        submitInlineLogin(harness);
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.REVIEW');

        expect(login).toHaveBeenCalledTimes(2);
        expect(login).toHaveBeenLastCalledWith({ email: EMAIL, password: PASSWORD, rememberMe: false, expectedUserId: ACCOUNT });
        expect(auth.transferTelegramDiary).toHaveBeenLastCalledWith(TICKET, undefined);
        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
    });

    it('shows the restricted account error and still lets the user keep the diaries separate', async () => {
        const { auth, harness, navigation } = await openVerificationAsync();
        auth.transferTelegramDiary.mockReturnValueOnce(transferFailure('User.TelegramDiaryRestrictedAccount'));
        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.SIGN_IN').click();
        harness.detectChanges();
        submitInlineLogin(harness);
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.RESTRICTED_ACCOUNT');
        expect(verificationPage(harness).querySelector('fd-auth')).toBeNull();
        expect(verificationPage(harness).textContent).not.toContain('AUTH.TELEGRAM_DIARY.MERGE');

        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.KEEP_SEPARATE').click();
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.CANCELLED');
        expect(verificationPage(harness).querySelector('fd-telegram-diary-transfer')).toBeNull();
        expect(auth.transferTelegramDiary).toHaveBeenCalledExactlyOnceWith(TICKET, undefined);
        verificationControl(harness, 'AUTH.VERIFY.BACK_TO_LOGIN').click();
        expect(navigation.navigateToAuthAsync).toHaveBeenCalledExactlyOnceWith('login');
    });
});

describe('Choosing another email on the verification route', () => {
    it('sends a signed-out reader to sign in with a profile return path to choose a different backup email', async () => {
        const { auth } = createVerificationMocks({ owner: null });
        TestBed.configureTestingModule({
            providers: [
                provideTranslateTesting(),
                provideRouter([
                    { path: 'verify-email', component: EmailVerificationComponent },
                    { path: '', component: SignInDestinationFixture },
                ]),
                { provide: AuthService, useValue: auth },
                { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } },
            ],
        });
        const harness = await RouterTestingHarness.create(`${VERIFY_URL}?userId=${SOURCE}&token=${PROOF}`);
        const router = TestBed.inject(Router);
        await vi.waitFor(() => {
            expect(router.url).toBe(VERIFY_URL);
        });

        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.USE_ANOTHER_EMAIL').click();

        await vi.waitFor(() => {
            expect(router.url).toBe('/?auth=login&returnUrl=%2Fprofile');
        });
        harness.detectChanges();
        expect(verificationPage(harness).querySelector('h1')?.textContent).toBe('Sign in');
        expect(auth.verifyTelegramBackupEmail).toHaveBeenCalledExactlyOnceWith(SOURCE, PROOF);
        expect(auth.logoutForAccountSwitchAsync).not.toHaveBeenCalled();
        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        expect(auth.transferTelegramDiary).not.toHaveBeenCalled();
    });

    it('takes the current backup-email owner straight to their profile', async () => {
        const { auth, harness, navigation } = await openVerificationAsync({ owner: SOURCE });
        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.USE_ANOTHER_EMAIL').click();
        await expectVerificationTextAsync(harness, 'AUTH.TELEGRAM_DIARY.CANCELLED');
        expect(navigation.navigateToProfileAsync).toHaveBeenCalledOnce();
        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        expect(auth.logoutForAccountSwitchAsync).not.toHaveBeenCalled();
    });

    it('requests an owned account switch with a profile return path and no separate landing or sign-in action', async () => {
        const { auth, harness, navigation } = await openVerificationAsync();
        let finishLogout: () => void = () => {};
        const logout = new Promise<void>(resolve => {
            finishLogout = resolve;
        });
        auth.logoutForAccountSwitchAsync.mockReturnValueOnce(logout);
        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.USE_ANOTHER_EMAIL').click();
        expect(auth.logoutForAccountSwitchAsync).toHaveBeenCalledExactlyOnceWith('/profile', expect.objectContaining({ destroyed: false }));
        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();

        finishLogout();
        await logout;
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
    });

    it('does not redirect from the abandoned verification page when sign-out finishes', async () => {
        const { auth, harness, navigation } = await openVerificationAsync();
        let finishLogout: () => void = () => {};
        const logout = new Promise<void>(resolve => {
            finishLogout = resolve;
        });
        auth.logoutForAccountSwitchAsync.mockReturnValueOnce(logout);
        verificationControl(harness, 'AUTH.TELEGRAM_DIARY.USE_ANOTHER_EMAIL').click();
        const owner = auth.logoutForAccountSwitchAsync.mock.lastCall?.[1];
        expect(owner?.destroyed).toBe(false);

        await harness.navigateByUrl(OTHER_URL, DiaryDestinationFixture);
        finishLogout();
        await logout;

        expect(owner?.destroyed).toBe(true);
        expect(verificationPage(harness).querySelector('h1')?.textContent).toBe('Diary');
        expect(navigation.navigateToAuthAsync).not.toHaveBeenCalled();
    });
});

it('retries email verification with the resolved link only once and then continues home', async () => {
    const request = new Subject<void>();
    const { auth, harness, navigation } = await openVerificationAsync({ standard: request });
    expect(verificationPage(harness).textContent).toContain('AUTH.VERIFY.PENDING');
    expect(auth.verifyEmail).toHaveBeenCalledExactlyOnceWith(ACCOUNT, 'regular-fixture');
    const retryRequest = new Subject<void>();
    auth.verifyEmail.mockReturnValueOnce(retryRequest);
    request.error(new Error('temporary verification outage'));
    await expectVerificationTextAsync(harness, 'AUTH.VERIFY.ERROR_GENERIC');

    const retry = verificationControl(harness, 'AUTH.VERIFY.RETRY');
    retry.click();
    expect(retry.disabled).toBe(false);
    retry.click();
    expect(auth.verifyEmail).toHaveBeenCalledTimes(2);

    retryRequest.next(undefined);
    retryRequest.complete();
    await expectVerificationTextAsync(harness, 'AUTH.VERIFY.SUCCESS');
    expect(auth.verifyEmail).toHaveBeenCalledTimes(2);
    expect(auth.verifyEmail).toHaveBeenLastCalledWith(ACCOUNT, 'regular-fixture');
    expect(auth.verifyTelegramBackupEmail).not.toHaveBeenCalled();
    verificationControl(harness, 'AUTH.VERIFY.CONTINUE').click();
    expect(navigation.navigateToHomeAsync).toHaveBeenCalledOnce();
});

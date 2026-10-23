import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { merge, Subscription } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { EmailVerificationCooldownService } from '../../../../shared/auth/email-verification-cooldown.service';
import { SessionEventsService } from '../../../../shared/auth/session-events.service';
import { MS_PER_SECOND } from '../../../../shared/lib/time.constants';
import { getRecordProperty, getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import { UserFacade } from '../../../../shared/lib/user.facade';
import { BrowserWindowService } from '../../../../shared/platform/browser-window.service';
import { EmailVerificationRealtimeService } from '../../lib/email-verification-realtime.service';

@Component({
    selector: 'fd-email-verification-pending',
    imports: [TranslatePipe, FdUiCardComponent, FdUiButtonComponent],
    templateUrl: './email-verification-pending.html',
    styleUrls: ['./email-verification-pending.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailVerificationPendingComponent {
    private readonly userFacade = inject(UserFacade);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly realtimeService = inject(EmailVerificationRealtimeService);
    private readonly route = inject(ActivatedRoute);
    private readonly cooldown = inject(EmailVerificationCooldownService);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly pageWork = new Subscription();
    private readonly shouldAutoResend = this.route.snapshot.queryParamMap.get('autoResend') === 'true';
    private readonly userId = this.authService.getUserId();
    private autoResendAttempted = false;
    private verificationCompleted = false;
    private cooldownIntervalId: number | null = null;

    protected readonly email = signal<string | null>(null);
    protected readonly statusMessage = signal<string | null>(null);
    protected readonly isChecking = signal(false);
    protected readonly isSending = signal(false);
    protected readonly resendCooldownSeconds = signal(0);

    public constructor() {
        this.destroyRef.onDestroy(() => {
            this.invalidatePage();
        });
        if (!this.authService.isAuthenticated()) {
            void this.navigationService.navigateToAuthAsync('login');
            return;
        }
        this.pageWork.add(
            merge(this.sessionEvents.authenticated$, this.sessionEvents.sessionEnded$, this.sessionEvents.localSessionInvalidating$)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(() => {
                    this.invalidatePage();
                }),
        );
        if (!this.ensureCurrentOwner()) {
            return;
        }
        this.startResendCooldown();
        this.startRealtime();
        this.loadCurrentUser();
    }

    protected onRefreshStatus(): void {
        if (!this.ensureCurrentOwner() || this.verificationCompleted) {
            return;
        }
        this.isChecking.set(true);
        this.pageWork.add(
            this.userFacade
                .getInfo()
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(user => {
                    this.isChecking.set(false);
                    if (!this.ensureCurrentOwner() || this.verificationCompleted) {
                        return;
                    }
                    if (user === null) {
                        this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.ERROR'));
                        return;
                    }
                    if (user.id !== this.userId) {
                        return;
                    }
                    this.email.set(user.email);
                    this.verificationCompleted = user.isEmailConfirmed;
                    this.authService.setEmailConfirmed(user.isEmailConfirmed);
                    if (user.isEmailConfirmed) {
                        this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.SUCCESS'));
                        void this.navigationService.navigateToHomeAsync();
                    } else {
                        this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.NOT_CONFIRMED'));
                    }
                }),
        );
    }

    protected onResendEmail(): void {
        this.sendVerificationEmail(false);
    }

    protected async onUseAnotherEmailAsync(): Promise<void> {
        if (!this.ensureCurrentOwner()) {
            return;
        }
        this.invalidatePage();
        await this.authService.onLogoutAsync(true);
    }

    private sendVerificationEmail(isAutomatic: boolean): void {
        if (!this.ensureCurrentOwner() || this.verificationCompleted || this.isSending() || this.resendCooldownSeconds() > 0) {
            return;
        }
        this.isSending.set(true);
        if (!isAutomatic) {
            this.statusMessage.set(null);
        }
        this.pageWork.add(
            this.authService
                .resendEmailVerification()
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({
                    next: () => {
                        this.isSending.set(false);
                        if (!this.ensureCurrentOwner() || this.verificationCompleted) {
                            return;
                        }
                        this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.RESENT'));
                        this.startResendCooldown();
                    },
                    error: (error: unknown) => {
                        this.handleResendError(error, isAutomatic);
                    },
                }),
        );
    }

    private handleResendError(error: unknown, isAutomatic: boolean): void {
        this.isSending.set(false);
        if (!this.ensureCurrentOwner() || this.verificationCompleted) {
            return;
        }
        const body = getRecordProperty(error, 'error');
        const fields = getRecordProperty(getRecordProperty(body, 'errors'), 'EmailVerification');
        if (getStringProperty(body, 'error') === 'Validation.Invalid' && Array.isArray(fields)) {
            this.cooldown.markSent(this.userId);
            this.startResendCooldown();
            this.statusMessage.set(null);
            return;
        }
        if (isAutomatic) {
            return;
        }
        this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.RESEND_ERROR'));
    }

    private tryAutoResendEmail(): void {
        if (!this.shouldAutoResend || this.autoResendAttempted || this.resendCooldownSeconds() > 0) {
            return;
        }

        this.autoResendAttempted = true;
        this.sendVerificationEmail(true);
    }

    private startResendCooldown(): void {
        if (!this.ensureCurrentOwner()) {
            return;
        }
        this.browserWindow.clearInterval(this.cooldownIntervalId);
        this.resendCooldownSeconds.set(this.cooldown.remainingSeconds(this.userId));
        if (this.resendCooldownSeconds() === 0) {
            return;
        }
        this.cooldownIntervalId = this.browserWindow.setInterval(() => {
            if (!this.ensureCurrentOwner()) {
                return;
            }
            this.resendCooldownSeconds.set(this.cooldown.remainingSeconds(this.userId));
            if (this.resendCooldownSeconds() === 0) {
                this.browserWindow.clearInterval(this.cooldownIntervalId);
            }
        }, MS_PER_SECOND);
    }

    private loadCurrentUser(): void {
        this.pageWork.add(
            this.userFacade
                .getInfo()
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe(user => {
                    if (!this.ensureCurrentOwner() || this.verificationCompleted) {
                        return;
                    }
                    if (user?.id !== this.userId) {
                        return;
                    }
                    this.startResendCooldown();
                    this.email.set(user.email);
                    this.verificationCompleted = user.isEmailConfirmed;
                    this.authService.setEmailConfirmed(user.isEmailConfirmed);
                    if (user.isEmailConfirmed) {
                        void this.navigationService.navigateToHomeAsync();
                    } else {
                        this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.INITIAL'));
                        this.tryAutoResendEmail();
                    }
                }),
        );
    }

    private startRealtime(): void {
        this.pageWork.add((): void => {
            void this.realtimeService.disconnectAsync().catch(() => {
                // Transport shutdown is best effort after leaving the verification page.
            });
        });
        void this.realtimeService
            .connectAsync(this.authService, () => {
                if (!this.ensureCurrentOwner() || this.verificationCompleted) {
                    return;
                }
                this.verificationCompleted = true;
                this.isChecking.set(false);
                this.authService.setEmailConfirmed(true);
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.SUCCESS'));
                void this.navigationService.navigateToHomeAsync();
            })
            .catch(() => {
                // Realtime verification is best effort; manual refresh and resend remain available.
            });
    }

    private ensureCurrentOwner(): boolean {
        if (
            !this.pageWork.closed &&
            !this.destroyRef.destroyed &&
            this.userId !== null &&
            this.authService.isAuthenticated() &&
            this.authService.getUserId() === this.userId
        ) {
            return true;
        }
        this.invalidatePage();
        return false;
    }

    private invalidatePage(): void {
        if (this.pageWork.closed) {
            return;
        }
        this.pageWork.unsubscribe();
        this.browserWindow.clearInterval(this.cooldownIntervalId);
        this.cooldownIntervalId = null;
        this.email.set(null);
        this.statusMessage.set(null);
        this.isChecking.set(false);
        this.isSending.set(false);
        this.resendCooldownSeconds.set(0);
    }
}

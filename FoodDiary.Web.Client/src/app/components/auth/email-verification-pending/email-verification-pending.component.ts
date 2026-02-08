import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { UserService } from '../../../services/user.service';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EmailVerificationRealtimeService } from '../../../services/email-verification-realtime.service';

@Component({
    selector: 'fd-email-verification-pending',
    imports: [TranslateModule, FdUiCardComponent, FdUiButtonComponent],
    templateUrl: './email-verification-pending.component.html',
    styleUrls: ['./email-verification-pending.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailVerificationPendingComponent {
    private readonly userService = inject(UserService);
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly realtimeService = inject(EmailVerificationRealtimeService);

    public readonly email = signal<string | null>(null);
    public readonly statusMessage = signal<string | null>(null);
    public readonly isChecking = signal(false);
    public readonly isSending = signal(false);
    public readonly resendCooldownSeconds = signal(0);

    public constructor() {
        if (!this.authService.isAuthenticated()) {
            void this.navigationService.navigateToAuth('login');
            return;
        }
        this.startRealtime();
        this.loadCurrentUser();
    }

    public onRefreshStatus(): void {
        this.isChecking.set(true);
        this.userService.getInfo().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(user => {
            this.isChecking.set(false);
            if (!user) {
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.ERROR'));
                return;
            }
            this.email.set(user.email);
            this.authService.setEmailConfirmed(user.isEmailConfirmed);
            if (user.isEmailConfirmed) {
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.SUCCESS'));
                void this.navigationService.navigateToHome();
            } else {
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.NOT_CONFIRMED'));
            }
        });
    }

    public onResendEmail(): void {
        this.isSending.set(true);
        this.statusMessage.set(null);
        this.authService.resendEmailVerification().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
                this.isSending.set(false);
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.RESENT'));
                this.startResendCooldown();
            },
            error: () => {
                this.isSending.set(false);
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.RESEND_ERROR'));
            },
        });
    }

    private startResendCooldown(seconds = 60): void {
        this.resendCooldownSeconds.set(seconds);
        const intervalId = window.setInterval(() => {
            const remaining = this.resendCooldownSeconds();
            if (remaining <= 1) {
                this.resendCooldownSeconds.set(0);
                window.clearInterval(intervalId);
                return;
            }
            this.resendCooldownSeconds.set(remaining - 1);
        }, 1000);

        this.destroyRef.onDestroy(() => {
            window.clearInterval(intervalId);
        });
    }

    private loadCurrentUser(): void {
        this.userService.getInfo().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(user => {
            if (!user) {
                return;
            }
            this.email.set(user.email);
            this.authService.setEmailConfirmed(user.isEmailConfirmed);
            if (user.isEmailConfirmed) {
                void this.navigationService.navigateToHome();
            } else {
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.INITIAL'));
            }
        });
    }

    private startRealtime(): void {
        this.realtimeService
            .connect(this.authService, () => {
                this.authService.setEmailConfirmed(true);
                this.statusMessage.set(this.translateService.instant('AUTH.VERIFY_PENDING.SUCCESS'));
                void this.navigationService.navigateToHome();
            })
            .then(() => {
                this.destroyRef.onDestroy(() => {
                    void this.realtimeService.disconnect();
                });
            });
    }
}

import { Location } from '@angular/common';
import { afterNextRender, ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { map } from 'rxjs';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { getRecordProperty, getStringProperty } from '../../../../shared/lib/unknown-value.utils';
import { TelegramDiaryTransferComponent } from './telegram-diary-transfer';

type VerificationLink = Readonly<{ userId: string; token: string }>;
type VerificationState =
    | { kind: 'pending' | 'success' | 'cancelled'; errorMessage: null }
    | { kind: 'invalid'; errorMessage: string }
    | { kind: 'error'; errorMessage: string; link: VerificationLink };

@Component({
    selector: 'fd-email-verification',
    imports: [TranslatePipe, FdUiCardComponent, FdUiButtonComponent, TelegramDiaryTransferComponent],
    templateUrl: './email-verification.html',
    styleUrls: ['./email-verification.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailVerificationComponent {
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly location = inject(Location);

    private readonly status = signal<VerificationState>({ kind: 'pending', errorMessage: null });
    protected readonly state = computed(() => this.status().kind);
    protected readonly isBusy = computed(() => this.state() === 'pending');
    protected readonly errorMessage = computed(() => this.status().errorMessage);
    protected readonly transferTicket = signal<string | null>(null);
    protected readonly emailToken = signal<{ userId: string | null; token: string | null }>({ userId: null, token: null });
    protected readonly isBackupEmail = computed(() => this.emailToken().token?.startsWith('telegram-backup.') === true);

    public constructor() {
        this.resolveAndVerify();
    }

    protected onContinue(): void {
        if (this.isBackupEmail()) {
            void this.authService.onLogoutAsync(true);
            return;
        }
        if (this.authService.isAuthenticated()) {
            void this.navigationService.navigateToHomeAsync();
        } else {
            void this.navigationService.navigateToAuthAsync('login');
        }
    }

    protected onRetry(): void {
        const status = this.status();
        if (status.kind === 'error') {
            this.verify(status.link);
        }
    }

    protected onBackToLogin(): void {
        void this.navigationService.navigateToAuthAsync('login');
    }

    protected onRequestNewLink(): void {
        if (this.authService.isAuthenticated() && this.authService.getUserId() === this.emailToken().userId && !this.isBackupEmail()) {
            void this.navigationService.navigateToEmailVerificationPendingAsync();
        } else {
            this.onBackToLogin();
        }
    }

    protected onKeepDiariesSeparate(): void {
        this.transferTicket.set(null);
        this.status.set({ kind: 'cancelled', errorMessage: null });
    }

    protected async onUseAnotherEmailAsync(): Promise<void> {
        this.onKeepDiariesSeparate();
        if (this.authService.getUserId() === this.emailToken().userId) {
            await this.navigationService.navigateToProfileAsync();
            return;
        }
        if (this.authService.isAuthenticated()) {
            await this.authService.logoutForAccountSwitchAsync('/profile', this.destroyRef);
            return;
        }
        await this.navigationService.navigateToAuthAsync('login', '/profile');
    }

    private resolveAndVerify(): void {
        const params = this.route.snapshot.queryParamMap;
        const userId = params.get('userId') ?? params.get('user') ?? params.get('id');
        const token = params.get('token');
        this.emailToken.set({ userId, token });

        if (userId === null || token === null || userId.length === 0 || token.length === 0) {
            this.status.set({ kind: 'invalid', errorMessage: this.translateService.instant('AUTH.VERIFY.ERROR_INVALID') });
            return;
        }
        if (this.isBackupEmail()) {
            this.location.replaceState('/verify-email');
            afterNextRender(() => {
                void this.router.navigateByUrl('/verify-email', { replaceUrl: true });
            });
        }

        this.verify({ userId, token });
    }

    private verify(link: VerificationLink): void {
        this.status.set({ kind: 'pending', errorMessage: null });
        const { userId, token } = link;

        const verification = this.isBackupEmail()
            ? this.authService.verifyTelegramBackupEmail(userId, token)
            : this.authService.verifyEmail(userId, token).pipe(map(() => null));
        verification.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: ticket => {
                this.status.set({ kind: 'success', errorMessage: null });
                if (ticket !== null) {
                    this.transferTicket.set(ticket);
                }
            },
            error: (error: unknown) => {
                const invalidToken = getStringProperty(getRecordProperty(error, 'error'), 'error') === 'Authentication.InvalidToken';
                const errorMessage = this.translateService.instant(
                    this.isBackupEmail()
                        ? 'USER_MANAGE.BACKUP_EMAIL_VERIFY_ERROR'
                        : invalidToken
                          ? 'AUTH.VERIFY.ERROR_INVALID'
                          : 'AUTH.VERIFY.ERROR_GENERIC',
                );
                this.status.set(invalidToken ? { kind: 'invalid', errorMessage } : { kind: 'error', errorMessage, link });
            },
        });
    }
}

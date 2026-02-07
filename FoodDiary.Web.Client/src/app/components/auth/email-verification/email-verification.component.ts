import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ActivatedRoute } from '@angular/router';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

type VerificationState = 'pending' | 'success' | 'error';

@Component({
    selector: 'fd-email-verification',
    imports: [TranslateModule, FdUiCardComponent, FdUiButtonComponent],
    templateUrl: './email-verification.component.html',
    styleUrls: ['./email-verification.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmailVerificationComponent {
    private readonly authService = inject(AuthService);
    private readonly navigationService = inject(NavigationService);
    private readonly route = inject(ActivatedRoute);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly state = signal<VerificationState>('pending');
    public readonly isBusy = signal(false);
    public readonly errorMessage = signal<string | null>(null);
    public readonly emailToken = signal<{ userId: string | null; token: string | null }>({ userId: null, token: null });

    public constructor() {
        this.resolveAndVerify();
    }

    public onContinue(): void {
        if (this.authService.isAuthenticated()) {
            void this.navigationService.navigateToHome();
        } else {
            void this.navigationService.navigateToAuth('login');
        }
    }

    public onRetry(): void {
        const { userId, token } = this.emailToken();
        if (!userId || !token) {
            this.errorMessage.set(this.translateService.instant('AUTH.VERIFY.ERROR_INVALID'));
            return;
        }
        this.verify(userId, token);
    }

    private resolveAndVerify(): void {
        const params = this.route.snapshot.queryParamMap;
        const userId = params.get('userId') ?? params.get('user') ?? params.get('id');
        const token = params.get('token');
        this.emailToken.set({ userId, token });

        if (!userId || !token) {
            this.state.set('error');
            this.errorMessage.set(this.translateService.instant('AUTH.VERIFY.ERROR_INVALID'));
            return;
        }

        this.verify(userId, token);
    }

    private verify(userId: string, token: string): void {
        this.isBusy.set(true);
        this.state.set('pending');
        this.errorMessage.set(null);

        this.authService
            .verifyEmail(userId, token)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: success => {
                    this.isBusy.set(false);
                    if (success) {
                        this.state.set('success');
                        return;
                    }
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('AUTH.VERIFY.ERROR_GENERIC'));
                },
                error: () => {
                    this.isBusy.set(false);
                    this.state.set('error');
                    this.errorMessage.set(this.translateService.instant('AUTH.VERIFY.ERROR_GENERIC'));
                },
            });
    }
}

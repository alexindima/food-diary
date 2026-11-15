import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { AdminAuthService } from '../../admin-auth/contracts/admin-session';
import { adminSignInAccount, adminSignInAccountFragment, adminSignInReturnUrl } from '../../admin-auth/contracts/admin-sign-in-navigation';

@Component({
    selector: 'fd-admin-unauthorized',
    imports: [FdUiButtonComponent, TranslatePipe],
    templateUrl: './unauthorized.html',
    styleUrl: './unauthorized.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnauthorizedComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly authService = inject(AdminAuthService);
    private readonly document = inject(DOCUMENT);
    private readonly account = adminSignInAccount(this.route.snapshot.fragment);

    protected readonly reason: string | null;
    protected readonly returnUrl: string;
    private readonly rejectedReason = signal<string | null>(null);
    protected readonly retryBusy = signal(false);
    protected readonly effectiveReason = computed(() => this.rejectedReason() ?? this.reason);
    protected readonly restoreUnavailable = computed(
        () => this.rejectedReason() === null && (this.effectiveReason() === 'unavailable' || this.authService.sessionUnavailable()),
    );
    protected readonly rateLimited = computed(() => this.effectiveReason() === 'unauthenticated' && this.authService.ssoRateLimited());

    public constructor() {
        this.reason = this.route.snapshot.queryParamMap.get('reason');
        this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';

        if (this.reason !== 'unauthenticated') {
            return;
        }

        if (this.returnUrl.length === 0) {
            return;
        }

        void this.tryRecoverFromSsoAsync(this.returnUrl);
    }

    protected goToLogin(): void {
        const returnUrl = adminSignInReturnUrl(this.returnUrl, this.document.location.origin);
        void this.router.navigate(['/sign-in'], {
            queryParams: { returnUrl, confirm: '1' },
            fragment: adminSignInAccountFragment(this.account),
        });
    }

    protected async retryRestoreAsync(): Promise<void> {
        if (this.retryBusy()) {
            return;
        }
        this.retryBusy.set(true);
        try {
            await this.authService.restoreSessionAsync(true);
            if (!this.authService.sessionUnavailable()) {
                if (this.authService.isAdmin()) {
                    const returnUrl = adminSignInReturnUrl(this.returnUrl, this.document.location.origin);
                    if (this.account !== null && this.authService.getUserId() !== this.account) {
                        await this.router.navigate(['/sign-in'], {
                            queryParams: { returnUrl },
                            fragment: adminSignInAccountFragment(this.account),
                            replaceUrl: true,
                        });
                    } else {
                        await this.router.navigateByUrl(returnUrl, { replaceUrl: true });
                    }
                } else {
                    this.rejectedReason.set(this.authService.isAuthenticated() ? 'forbidden' : 'unauthenticated');
                }
            }
        } finally {
            this.retryBusy.set(false);
        }
    }

    private async tryRecoverFromSsoAsync(returnUrl: string): Promise<void> {
        const cleanedUrl: unknown = await this.authService.tryApplySsoFromReturnUrlAsync(returnUrl);
        if (typeof cleanedUrl !== 'string' || cleanedUrl.length === 0) {
            return;
        }

        await this.router.navigateByUrl(cleanedUrl, { replaceUrl: true });
    }
}

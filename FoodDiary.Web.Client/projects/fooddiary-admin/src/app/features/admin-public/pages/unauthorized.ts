import { DOCUMENT } from '@angular/common';
import { afterNextRender, ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminAuthService } from '../../admin-auth/contracts/admin-session';
import { adminSignInAccount, adminSignInAccountFragment, adminSignInReturnUrl } from '../../admin-auth/contracts/admin-sign-in-navigation';

type UnauthorizedEntry = { reason: string | undefined; returnUrl: string };

@Component({
    selector: 'fd-admin-unauthorized',
    imports: [FdUiButtonComponent, TranslatePipe],
    templateUrl: './unauthorized.html',
    styleUrl: './unauthorized.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnauthorizedComponent {
    public readonly reason = input(undefined, { transform: firstAdminQueryValue });
    public readonly returnUrl = input(undefined, { transform: firstAdminQueryValue });

    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly authService = inject(AdminAuthService);
    private readonly document = inject(DOCUMENT);
    private readonly account = adminSignInAccount(this.route.snapshot.fragment);

    private readonly entry = linkedSignal<UnauthorizedEntry, UnauthorizedEntry>({
        source: () => ({ reason: this.reason(), returnUrl: this.returnUrl() ?? '/' }),
        computation: (current, previous) => previous?.value ?? current,
    }).asReadonly();
    private readonly rejectedReason = signal<string | null>(null);
    protected readonly retryBusy = signal(false);
    protected readonly effectiveReason = computed(() => this.rejectedReason() ?? this.entry().reason);
    protected readonly restoreUnavailable = computed(
        () => this.rejectedReason() === null && (this.effectiveReason() === 'unavailable' || this.authService.sessionUnavailable()),
    );
    protected readonly rateLimited = computed(() => this.effectiveReason() === 'unauthenticated' && this.authService.ssoRateLimited());

    public constructor() {
        afterNextRender(() => {
            const { reason, returnUrl } = this.entry();
            if (reason === 'unauthenticated' && returnUrl.length > 0) {
                void this.tryRecoverFromSsoAsync(returnUrl);
            }
        });
    }

    protected goToLogin(): void {
        const returnUrl = adminSignInReturnUrl(this.entry().returnUrl, this.document.location.origin);
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
                    const returnUrl = adminSignInReturnUrl(this.entry().returnUrl, this.document.location.origin);
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

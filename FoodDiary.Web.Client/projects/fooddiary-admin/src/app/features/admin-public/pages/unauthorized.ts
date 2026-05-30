import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';

import { environment } from '../../../../environments/environment';
import { AdminAuthService } from '../../admin-auth/lib/admin-auth.service';

@Component({
    selector: 'fd-admin-unauthorized',
    imports: [FdUiButtonComponent],
    templateUrl: './unauthorized.html',
    styleUrl: './unauthorized.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnauthorizedComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly authService = inject(AdminAuthService);
    private readonly document = inject(DOCUMENT);

    protected readonly reason: string | null;
    protected readonly returnUrl: string;

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
        const url = new URL('/', environment.mainAppUrl);
        url.searchParams.set('auth', 'login');
        const adminReturnUrl = this.normalizeReturnUrl(this.returnUrl);
        if (adminReturnUrl.length > 0) {
            url.searchParams.set('adminReturnUrl', adminReturnUrl);
        }
        this.document.defaultView?.location.assign(url.toString());
    }

    private async tryRecoverFromSsoAsync(returnUrl: string): Promise<void> {
        const cleanedUrl: unknown = await this.authService.tryApplySsoFromReturnUrlAsync(returnUrl);
        if (typeof cleanedUrl !== 'string' || cleanedUrl.length === 0) {
            return;
        }

        await this.router.navigateByUrl(cleanedUrl, { replaceUrl: true });
    }

    private normalizeReturnUrl(value: string): string {
        if (value.length === 0) {
            return '/';
        }

        const decoded = this.safeDecode(value);
        if (decoded.includes('returnUrl=')) {
            return '/';
        }

        return decoded.startsWith('/') ? decoded : '/';
    }

    private safeDecode(value: string): string {
        try {
            return decodeURIComponent(value);
        } catch {
            return value;
        }
    }
}

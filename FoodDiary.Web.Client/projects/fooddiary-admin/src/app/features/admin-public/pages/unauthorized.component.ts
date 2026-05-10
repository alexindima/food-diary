import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { environment } from '../../../../environments/environment';
import { AdminAuthService } from '../../admin-auth/lib/admin-auth.service';

@Component({
    selector: 'fd-admin-unauthorized',
    standalone: true,
    imports: [FdUiButtonComponent],
    templateUrl: './unauthorized.component.html',
    styleUrl: './unauthorized.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnauthorizedComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly authService = inject(AdminAuthService);

    public readonly reason: string | null;
    public readonly returnUrl: string;

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

    public goToLogin(): void {
        const url = new URL('/auth/login', environment.mainAppUrl);
        const adminReturnUrl = this.normalizeReturnUrl(this.returnUrl);
        if (adminReturnUrl.length > 0) {
            url.searchParams.set('adminReturnUrl', adminReturnUrl);
        }
        window.location.assign(url.toString());
    }

    private async tryRecoverFromSsoAsync(returnUrl: string): Promise<void> {
        const cleanedUrl = (await this.authService.tryApplySsoFromReturnUrlAsync(returnUrl)) as string | null | undefined;
        if (cleanedUrl === null || cleanedUrl === undefined || cleanedUrl.length === 0) {
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

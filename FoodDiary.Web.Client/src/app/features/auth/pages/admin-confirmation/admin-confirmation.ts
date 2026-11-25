import { DOCUMENT } from '@angular/common';
import { afterNextRender, ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../services/auth.service';
import { buildAdminSignInUrl, normalizeAdminReturnUrl } from '../../components/auth/auth-lib/auth-admin-return-url.utils';

@Component({
    selector: 'fd-admin-confirmation',
    imports: [TranslatePipe, FdUiCardComponent, RouterLink],
    templateUrl: './admin-confirmation.html',
    styleUrl: './admin-confirmation.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminConfirmationComponent {
    public readonly adminReturnUrl = input(undefined, {
        transform: (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value),
    });
    private readonly auth = inject(AuthService);
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly failed = signal(false);

    public constructor() {
        afterNextRender(() => void this.redirectToAdminAsync());
    }

    private async redirectToAdminAsync(): Promise<void> {
        const adminAppUrl = environment.adminAppUrl;
        if (adminAppUrl === undefined || adminAppUrl.length === 0) {
            this.failed.set(true);
            return;
        }

        const adminOrigin = new URL(adminAppUrl, this.document.location.origin);
        const path = normalizeAdminReturnUrl(this.adminReturnUrl() ?? '/', adminOrigin.href);
        const expectedUserId = new URLSearchParams(this.document.location.hash.slice(1)).get('account');
        await this.auth.ensureSessionReadyAsync();
        if (this.destroyRef.destroyed) {
            return;
        }
        const diaryAdmin = this.auth.isAdmin() ? this.auth.getUserId() : null;
        const handoff = await this.startSsoAsync(new URL(path, adminOrigin), diaryAdmin, expectedUserId);
        this.replaceLocationIfActive(handoff ?? buildAdminSignInUrl(adminOrigin, path, expectedUserId ?? diaryAdmin));
    }

    private replaceLocationIfActive(destination: URL): void {
        if (!this.destroyRef.destroyed) {
            this.document.location.replace(destination.href);
        }
    }

    private async startSsoAsync(destination: URL, diaryAdmin: string | null, expectedUserId: string | null): Promise<URL | null> {
        if (diaryAdmin === null || (expectedUserId !== null && diaryAdmin !== expectedUserId)) {
            return null;
        }
        try {
            const { code } = await firstValueFrom(this.auth.startAdminSso().pipe(takeUntilDestroyed(this.destroyRef)));
            destination.hash = new URLSearchParams({ code, account: diaryAdmin }).toString();
            return destination;
        } catch {
            return null;
        }
    }
}

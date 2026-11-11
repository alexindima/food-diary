import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../services/auth.service';
import { optionalEntityId } from '../../../../shared/models/semantics/entity-id';
import { AuthComponent } from '../../components/auth/auth';
import { normalizeAdminReturnUrl } from '../../components/auth/auth-lib/auth-admin-return-url.utils';

@Component({
    selector: 'fd-admin-confirmation',
    imports: [AuthComponent, TranslatePipe, FdUiCardComponent, FdUiButtonComponent],
    templateUrl: './admin-confirmation.html',
    styleUrl: './admin-confirmation.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminConfirmationComponent {
    private readonly auth = inject(AuthService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly document = inject(DOCUMENT);
    protected readonly userId = optionalEntityId<'user'>(this.auth.getUserId());
    protected readonly busy = signal(false);
    protected readonly failed = signal(false);

    protected back(): void {
        void this.router.navigate(['/profile']);
    }

    protected async continueAsync(): Promise<void> {
        this.busy.set(true);
        this.failed.set(false);
        try {
            if (this.userId === null || this.auth.getUserId() !== this.userId) {
                this.failed.set(true);
                return;
            }
            const adminAppUrl = environment.adminAppUrl ?? '';
            const path = normalizeAdminReturnUrl(
                this.route.snapshot.queryParamMap.get('adminReturnUrl') ?? '/',
                adminAppUrl,
                this.document.location.origin,
            );
            if (path === null || adminAppUrl.length === 0) {
                await this.router.navigate(['/profile']);
                return;
            }
            const response = await firstValueFrom(this.auth.startAdminSso());
            const url = new URL(path, adminAppUrl);
            url.hash = new URLSearchParams({ code: response.code }).toString();
            this.document.location.assign(url.toString());
        } catch {
            this.failed.set(true);
        } finally {
            this.busy.set(false);
        }
    }
}

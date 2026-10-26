import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiInlineAlertComponent } from 'fd-ui-kit';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { AuthRecoveryStateService } from '../../../shared/auth/auth-recovery-state.service';

@Component({
    selector: 'fd-auth-recovery-notice',
    imports: [TranslatePipe, FdUiInlineAlertComponent],
    templateUrl: './auth-recovery-notice.html',
    styleUrl: './auth-recovery-notice.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthRecoveryNoticeComponent {
    protected readonly recovery = inject(AuthRecoveryStateService);
    private readonly auth = inject(AuthService);
    private readonly navigation = inject(NavigationService);

    protected async retryAsync(): Promise<void> {
        if (this.recovery.busy() || !this.recovery.canRetry()) {
            return;
        }
        if (this.recovery.pendingLogout() !== null) {
            await this.retryLogoutAsync();
            return;
        }
        this.recovery.busy.set(true);
        try {
            const restored = await firstValueFrom(this.auth.refreshSession());
            const returnUrl = this.recovery.returnUrl;
            if (restored) {
                this.recovery.returnUrl = null;
                await this.navigation.navigateToReturnUrlAsync(returnUrl);
            } else if (this.recovery.issue() === null) {
                await this.navigation.navigateToAuthAsync('login', returnUrl ?? undefined);
            }
        } finally {
            this.recovery.busy.set(false);
        }
    }

    private async retryLogoutAsync(): Promise<void> {
        await this.auth.retryPendingLogoutAsync();
        if (this.recovery.issue() === null && !this.auth.isAuthenticated()) {
            const returnUrl = this.recovery.returnUrl;
            this.recovery.returnUrl = null;
            await this.navigation.navigateToAuthAsync('login', returnUrl ?? undefined);
        }
    }
}

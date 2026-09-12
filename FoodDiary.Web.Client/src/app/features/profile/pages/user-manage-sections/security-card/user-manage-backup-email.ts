import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { interval } from 'rxjs';

import { ProfileManageFacade } from '../../../lib/profile-manage.facade';

const MAX_EMAIL_LENGTH = 254;
const SECOND_MS = 1000;

@Component({
    selector: 'fd-user-manage-backup-email',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './user-manage-backup-email.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManageBackupEmailComponent {
    protected readonly facade = inject(ProfileManageFacade);
    protected readonly email = signal(this.facade.backupEmailSentTo() ?? '');
    protected readonly visible = computed(() => this.facade.user()?.hasTelegramIdentity === true);
    protected readonly accountEmail = computed(() => this.facade.user()?.email ?? null);
    protected readonly confirmed = computed(() => this.facade.user()?.isEmailConfirmed === true);
    private readonly now = signal(Date.now());
    protected readonly resendSeconds = computed(() => Math.max(0, Math.ceil((this.facade.backupEmailResendAt() - this.now()) / SECOND_MS)));

    public constructor() {
        interval(SECOND_MS)
            .pipe(takeUntilDestroyed())
            .subscribe(() => {
                this.now.set(Date.now());
            });
    }
    protected readonly validEmail = computed(
        () => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email().trim()) && this.email().trim().length <= MAX_EMAIL_LENGTH,
    );

    protected setEmail(value: string | number | null): void {
        this.email.set(typeof value === 'string' ? value : '');
    }

    protected submit(): void {
        if (this.validEmail() && !this.facade.isRequestingBackupEmail() && this.resendSeconds() === 0) {
            void this.facade.requestBackupEmailAsync(this.email());
        }
    }
}

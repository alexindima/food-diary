import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit';
import { firstValueFrom, merge, takeUntil } from 'rxjs';

import {
    ConfirmDeleteDialogComponent,
    type ConfirmDeleteDialogData,
} from '../../../components/shared/confirm-delete-dialog/confirm-delete-dialog';
import { AuthService } from '../../../services/auth.service';
import { UserService } from '../../../shared/api/user.service';
import { SessionEventsService } from '../../../shared/auth/session-events.service';

@Injectable()
export class AccountDeletionFacade {
    private readonly users = inject(UserService);
    private readonly auth = inject(AuthService);
    private readonly dialogs = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly sessions = inject(SessionEventsService);
    private readonly sessionChanged = merge(this.sessions.authenticated$, this.sessions.sessionEnded$);
    private sessionVersion = 0;
    public readonly busy = signal(false);
    public readonly failed = signal(false);

    public constructor() {
        this.sessionChanged.pipe(takeUntilDestroyed()).subscribe(() => {
            this.sessionVersion++;
            this.busy.set(false);
            this.failed.set(false);
        });
        this.destroyRef.onDestroy(() => this.sessionVersion++);
    }

    public async confirmAsync(): Promise<void> {
        if (this.busy()) {
            return;
        }
        const session = this.sessionVersion;
        const userId = this.users.user()?.id;
        if (userId === undefined) {
            this.failed.set(true);
            return;
        }
        this.busy.set(true);
        this.failed.set(false);
        try {
            const data: ConfirmDeleteDialogData = {
                title: this.translate.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM_TITLE'),
                message: this.translate.instant('USER_MANAGE.DELETE_ACCOUNT_CONFIRM_MESSAGE'),
                confirmLabel: this.translate.instant('PRIVACY_CHOICES.DELETE_ACCOUNT'),
                cancelLabel: this.translate.instant('COMMON.CANCEL'),
            };
            const confirmed = await this.askAsync(data);
            if (confirmed !== true || !this.isCurrentUser(session, userId)) {
                return;
            }
            await this.deleteForSessionAsync(session);
        } catch {
            if (session === this.sessionVersion) {
                this.failed.set(true);
            }
        } finally {
            if (session === this.sessionVersion) {
                this.busy.set(false);
            }
        }
    }

    private isCurrentUser(session: number, userId: string): boolean {
        return session === this.sessionVersion && this.users.user()?.id === userId;
    }

    private async askAsync(data: ConfirmDeleteDialogData): Promise<boolean | undefined> {
        const dialog = this.dialogs.open<ConfirmDeleteDialogComponent, ConfirmDeleteDialogData, boolean>(ConfirmDeleteDialogComponent, {
            preset: 'confirm',
            data,
        });
        try {
            return await firstValueFrom(dialog.afterClosed().pipe(takeUntil(this.sessionChanged), takeUntilDestroyed(this.destroyRef)));
        } finally {
            dialog.close(false);
        }
    }

    private async deleteForSessionAsync(session: number): Promise<void> {
        const deleted = await firstValueFrom(
            this.users.deleteCurrentUser().pipe(takeUntil(this.sessionChanged), takeUntilDestroyed(this.destroyRef)),
        );
        if (session !== this.sessionVersion) {
            return;
        }
        if (deleted) {
            await this.auth.onLogoutAsync(true);
        } else {
            this.failed.set(true);
        }
    }
}

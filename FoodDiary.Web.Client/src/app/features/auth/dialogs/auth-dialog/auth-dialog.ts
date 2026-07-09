import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { AuthComponent } from '../../components/auth/auth';

@Component({
    selector: 'fd-auth-dialog',
    templateUrl: './auth-dialog.html',
    styleUrls: ['./auth-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogShellComponent, FdUiIconComponent, AuthComponent],
})
export class AuthDialogComponent {
    private readonly dialogRef = inject(FdUiDialogRef<AuthDialogComponent>, { optional: true });
    protected readonly data = inject<AuthDialogData | null>(FD_UI_DIALOG_DATA, {
        optional: true,
    }) ?? { mode: 'login', returnUrl: null, adminReturnUrl: null };

    protected close(): void {
        this.dialogRef?.close();
    }
}

type AuthDialogData = {
    mode: 'login' | 'register';
    returnUrl?: string | null;
    adminReturnUrl?: string | null;
};

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell';

import { AuthComponent } from '../../components/auth/auth';

@Component({
    selector: 'fd-auth-dialog',
    templateUrl: './auth-dialog.html',
    styleUrls: ['./auth-dialog.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogShellComponent, AuthComponent],
})
export class AuthDialogComponent {
    protected readonly data = inject<AuthDialogData | null>(FD_UI_DIALOG_DATA, {
        optional: true,
    }) ?? { mode: 'login', returnUrl: null, adminReturnUrl: null };
}

type AuthDialogData = {
    mode: 'login' | 'register';
    returnUrl?: string | null;
    adminReturnUrl?: string | null;
};

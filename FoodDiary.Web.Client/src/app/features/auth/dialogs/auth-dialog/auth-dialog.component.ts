import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { AuthComponent } from '../../components/auth/auth.component';

@Component({
    selector: 'fd-auth-dialog',
    standalone: true,
    templateUrl: './auth-dialog.component.html',
    styleUrls: ['./auth-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogShellComponent, AuthComponent],
})
export class AuthDialogComponent {
    public readonly data = inject<AuthDialogData | null>(FD_UI_DIALOG_DATA, {
        optional: true,
    }) ?? { mode: 'login', returnUrl: null, adminReturnUrl: null };
}

interface AuthDialogData {
    mode: 'login' | 'register';
    returnUrl?: string | null;
    adminReturnUrl?: string | null;
}

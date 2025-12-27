import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/material';
import { FdUiDialogShellComponent } from 'fd-ui-kit/dialog-shell/fd-ui-dialog-shell.component';
import { AuthComponent } from './auth.component';

@Component({
    selector: 'fd-auth-dialog',
    standalone: true,
    template: `
        <fd-ui-dialog-shell [title]="''" size="md" [dismissible]="false" [flush]="true">
            <fd-auth [useRouting]="false" [initialMode]="data.mode" />
        </fd-ui-dialog-shell>
    `,
    styleUrls: ['./auth-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogShellComponent, AuthComponent],
})
export class AuthDialogComponent {
    public readonly data: { mode: 'login' | 'register' } =
        inject<{ mode: 'login' | 'register' }>(FD_UI_DIALOG_DATA, { optional: true }) ?? { mode: 'login' };
}

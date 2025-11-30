import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/material';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { AuthComponent } from './auth.component';

@Component({
    selector: 'fd-auth-dialog',
    standalone: true,
    template: `
        <fd-ui-dialog class="auth-dialog-shell" [title]="''" size="md">
            <fd-auth [useRouting]="false" [initialMode]="data.mode" />
        </fd-ui-dialog>
    `,
    styleUrls: ['./auth-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FdUiDialogComponent, AuthComponent],
})
export class AuthDialogComponent {
    public readonly data: { mode: 'login' | 'register' } =
        inject<{ mode: 'login' | 'register' }>(FD_UI_DIALOG_DATA, { optional: true }) ?? { mode: 'login' };
}

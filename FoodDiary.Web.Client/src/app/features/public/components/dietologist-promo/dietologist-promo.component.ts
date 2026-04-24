import { Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

@Component({
    selector: 'fd-dietologist-promo',
    imports: [TranslateModule, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './dietologist-promo.component.html',
    styleUrl: './dietologist-promo.component.scss',
})
export class DietologistPromoComponent {
    private readonly fdDialogService = inject(FdUiDialogService);

    protected readonly workflowSteps = ['INVITE', 'SHARE', 'ADJUST'];
    protected readonly permissionKeys = ['MEALS', 'STATISTICS', 'WEIGHT', 'GOALS', 'FASTING'];

    protected async openAuth(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            data: { mode },
        });
    }
}

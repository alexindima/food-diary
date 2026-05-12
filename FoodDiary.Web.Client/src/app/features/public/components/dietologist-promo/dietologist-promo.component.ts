import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

@Component({
    selector: 'fd-dietologist-promo',
    imports: [TranslateModule, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './dietologist-promo.component.html',
    styleUrl: './dietologist-promo.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DietologistPromoComponent {
    private readonly fdDialogService = inject(FdUiDialogService);

    protected readonly workflowSteps = ['INVITE', 'SHARE', 'ADJUST'].map(key => ({
        key,
        titleKey: `LANDING_DIETOLOGIST.STEPS.${key}.TITLE`,
        textKey: `LANDING_DIETOLOGIST.STEPS.${key}.TEXT`,
    }));
    protected readonly permissions = ['MEALS', 'STATISTICS', 'WEIGHT', 'GOALS', 'FASTING'].map(key => ({
        key,
        labelKey: `LANDING_DIETOLOGIST.PERMISSIONS.${key}`,
    }));

    protected async openAuthAsync(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            autoFocus: mode === 'login' ? '#auth-login-email' : '#auth-register-email',
            data: { mode },
        });
    }
}

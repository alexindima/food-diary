import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

@Component({
    selector: 'fd-landing-steps',
    standalone: true,
    imports: [TranslateModule, FdUiButtonComponent],
    templateUrl: './landing-steps.component.html',
    styleUrls: ['./landing-steps.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LandingStepsComponent {
    private readonly fdDialogService = inject(FdUiDialogService);

    protected readonly stepKeys = ['STEP1', 'STEP2', 'STEP3'] as const;

    public async openAuthAsync(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            data: { mode },
        });
    }
}

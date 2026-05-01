import { Component } from '@angular/core';
import { inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

@Component({
    selector: 'fd-landing-cta',
    standalone: true,
    imports: [TranslateModule, FdUiButtonComponent],
    templateUrl: './landing-cta.component.html',
    styleUrls: ['./landing-cta.component.scss'],
})
export class LandingCtaComponent {
    private readonly fdDialogService = inject(FdUiDialogService);

    public async openAuth(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            data: { mode },
        });
    }
}

import { Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { AuthDialogComponent } from '../../auth/auth-dialog.component';
import { inject } from '@angular/core';

@Component({
    selector: 'fd-landing-cta',
    standalone: true,
    imports: [TranslateModule, FdUiButtonComponent],
    templateUrl: './landing-cta.component.html',
    styleUrls: ['./landing-cta.component.scss'],
})
export class LandingCtaComponent {
    private readonly fdDialogService = inject(FdUiDialogService);

    public openAuth(mode: 'login' | 'register'): void {
        this.fdDialogService.open(AuthDialogComponent, {
            size: 'md',
            data: { mode },
        });
    }
}

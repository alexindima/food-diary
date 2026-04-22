import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { HeroComponent } from '../../components/hero/hero.component';
import { FeaturesComponent } from '../../components/features/features.component';
import { LandingPreviewTourComponent } from '../../components/landing-preview-tour/landing-preview-tour.component';
import { LandingStepsComponent } from '../../components/landing-steps/landing-steps.component';
import { DietologistPromoComponent } from '../../components/dietologist-promo/dietologist-promo.component';

@Component({
    selector: 'fd-main',
    imports: [HeroComponent, FeaturesComponent, LandingPreviewTourComponent, LandingStepsComponent, DietologistPromoComponent],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.scss'],
})
export class MainComponent {
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly route = inject(ActivatedRoute);

    public constructor() {
        const path = this.route.snapshot.routeConfig?.path ?? '';
        if (path.startsWith('auth')) {
            const modeParam = this.route.snapshot.params['mode'];
            const mode: 'login' | 'register' = modeParam === 'register' ? 'register' : 'login';
            void this.openAuthDialog(mode);
        }
    }

    private async openAuthDialog(mode: 'login' | 'register'): Promise<void> {
        const { AuthDialogComponent } = await import('../../../auth/dialogs/auth-dialog/auth-dialog.component');
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const adminReturnUrl = this.route.snapshot.queryParamMap.get('adminReturnUrl');

        this.fdDialogService.open(AuthDialogComponent, {
            size: 'md',
            data: { mode, returnUrl, adminReturnUrl },
        });
    }
}

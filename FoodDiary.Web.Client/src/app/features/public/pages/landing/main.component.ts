import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { DietologistPromoComponent } from '../../components/dietologist-promo/dietologist-promo.component';
import { FeaturesComponent } from '../../components/features/features.component';
import { HeroComponent } from '../../components/hero/hero.component';
import { LandingFaqComponent } from '../../components/landing-faq/landing-faq.component';
import { LandingPreviewTourComponent } from '../../components/landing-preview-tour/landing-preview-tour.component';
import { LandingStepsComponent } from '../../components/landing-steps/landing-steps.component';
import { PublicAuthDialogService, type PublicAuthMode } from '../../lib/public-auth-dialog.service';
import { LANDING_FAQ_ITEMS, LANDING_SEO_GUIDES } from './landing-main.config';

@Component({
    selector: 'fd-main',
    imports: [
        HeroComponent,
        FeaturesComponent,
        LandingPreviewTourComponent,
        LandingStepsComponent,
        DietologistPromoComponent,
        LandingFaqComponent,
    ],
    templateUrl: './main.component.html',
    styleUrls: ['./main.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainComponent {
    private readonly authDialogService = inject(PublicAuthDialogService);
    private readonly route = inject(ActivatedRoute);
    protected readonly faqItems = LANDING_FAQ_ITEMS;
    protected readonly seoGuides = LANDING_SEO_GUIDES;

    public constructor() {
        const path = this.route.snapshot.routeConfig?.path ?? '';
        if (path.startsWith('auth')) {
            const modeParam = this.route.snapshot.paramMap.get('mode');
            const mode: PublicAuthMode = modeParam === 'register' ? 'register' : 'login';
            void this.openAuthDialogAsync(mode);
        }
    }

    private async openAuthDialogAsync(mode: PublicAuthMode): Promise<void> {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const adminReturnUrl = this.route.snapshot.queryParamMap.get('adminReturnUrl');

        await this.authDialogService.openAsync({ mode, returnUrl, adminReturnUrl });
    }
}

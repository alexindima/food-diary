import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
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
    private readonly authService = inject(AuthService);
    private readonly authDialogService = inject(PublicAuthDialogService);
    private readonly navigationService = inject(NavigationService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly faqItems = LANDING_FAQ_ITEMS;
    protected readonly seoGuides = LANDING_SEO_GUIDES;
    private authDialogOpen = false;

    public constructor() {
        effect(() => {
            if (!this.authService.isAuthReady() || !this.authService.isAuthenticated()) {
                return;
            }

            void this.navigationService.navigateToHomeAsync();
        });

        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            const authParam = params.get('auth');
            if (authParam === null) {
                return;
            }

            const mode: PublicAuthMode = authParam === 'register' ? 'register' : 'login';
            void this.openAuthDialogAsync(mode, params.get('returnUrl'), params.get('adminReturnUrl'));
        });
    }

    private async openAuthDialogAsync(mode: PublicAuthMode, returnUrl: string | null, adminReturnUrl: string | null): Promise<void> {
        if (this.authDialogOpen) {
            return;
        }

        this.authDialogOpen = true;
        const dialogRef = await this.authDialogService.openAsync({ mode, returnUrl, adminReturnUrl });
        dialogRef
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.authDialogOpen = false;
                if (this.authService.isAuthenticated()) {
                    return;
                }

                void this.router.navigate([], {
                    relativeTo: this.route,
                    queryParams: { auth: null, returnUrl: null, adminReturnUrl: null },
                    queryParamsHandling: 'merge',
                    replaceUrl: true,
                });
            });
    }
}

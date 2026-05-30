import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { DietologistPromoComponent } from '../../components/dietologist-promo/dietologist-promo';
import { FeaturesComponent } from '../../components/features/features';
import { HeroComponent } from '../../components/hero/hero';
import { LandingFaqComponent } from '../../components/landing-faq/landing-faq';
import { LandingPreviewTourComponent } from '../../components/landing-preview-tour/landing-preview-tour';
import { LandingStepsComponent } from '../../components/landing-steps/landing-steps';
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
    templateUrl: './main.html',
    styleUrls: ['./main.scss'],
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
    private handledAuthQueryKey: string | null = null;

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
                this.handledAuthQueryKey = null;
                return;
            }

            const mode: PublicAuthMode = authParam === 'register' ? 'register' : 'login';
            const returnUrl = params.get('returnUrl');
            const adminReturnUrl = params.get('adminReturnUrl');
            const authQueryKey = this.createAuthQueryKey(mode, returnUrl, adminReturnUrl);
            if (authQueryKey === this.handledAuthQueryKey) {
                return;
            }

            this.handledAuthQueryKey = authQueryKey;
            void this.openAuthDialogAsync(mode, returnUrl, adminReturnUrl);
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

    private createAuthQueryKey(mode: PublicAuthMode, returnUrl: string | null, adminReturnUrl: string | null): string {
        return JSON.stringify({ mode, returnUrl, adminReturnUrl });
    }
}

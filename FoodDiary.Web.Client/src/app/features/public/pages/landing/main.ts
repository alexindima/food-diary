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

type LandingAuthRequest = {
    key: string;
    mode: PublicAuthMode;
    returnUrl: string | null;
    adminReturnUrl: string | null;
};

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
    private authRequest: LandingAuthRequest | null = null;

    public constructor() {
        effect(() => {
            if (!this.authService.isAuthReady() || !this.authService.isAuthenticated()) {
                return;
            }

            if (this.authDialogOpen || this.route.snapshot.queryParamMap.has('auth')) {
                return;
            }

            void this.navigationService.navigateToHomeAsync();
        });

        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            const authParam = params.get('auth');
            if (authParam === null) {
                this.authRequest = null;
                return;
            }

            const mode: PublicAuthMode = authParam === 'register' ? 'register' : 'login';
            const returnUrl = params.get('returnUrl');
            const adminReturnUrl = params.get('adminReturnUrl');
            const key = JSON.stringify({ mode, returnUrl, adminReturnUrl });
            if (this.authRequest?.key === key) {
                return;
            }

            const request = { key, mode, returnUrl, adminReturnUrl };
            this.authRequest = request;
            void this.openAuthDialogAsync(request);
        });
    }

    private async openAuthDialogAsync(request: LandingAuthRequest): Promise<void> {
        if (this.authDialogOpen) {
            return;
        }

        this.authDialogOpen = true;
        const { mode, returnUrl, adminReturnUrl } = request;
        const dialogRef = await this.authDialogService.openAsync({
            mode,
            returnUrl,
            adminReturnUrl,
            destroyRef: this.destroyRef,
            canOpen: () => this.authRequest === request,
        });
        if (dialogRef === null || this.destroyRef.destroyed) {
            this.authDialogOpen = false;
            const replacement = this.authRequest;
            if (replacement !== null && replacement !== request && !this.destroyRef.destroyed) {
                void this.openAuthDialogAsync(replacement);
            }
            return;
        }

        dialogRef
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.authDialogOpen = false;
                if (this.destroyRef.destroyed || this.authService.isAuthenticated()) {
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

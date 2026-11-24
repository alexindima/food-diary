import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, untracked } from '@angular/core';
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

function firstQueryValue(value: string | string[] | undefined): string | undefined {
    return Array.isArray(value) ? value[0] : value;
}

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
    public readonly auth = input(undefined, { transform: firstQueryValue });
    public readonly returnUrl = input(undefined, { transform: firstQueryValue });
    public readonly adminReturnUrl = input(undefined, { transform: firstQueryValue });

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
    private readonly requestedAuth = computed((): LandingAuthRequest | null => {
        const authParam = this.auth();
        if (authParam === undefined) {
            return null;
        }
        const mode = authParam === 'register' ? 'register' : 'login';
        const returnUrl = this.returnUrl() ?? null;
        const adminReturnUrl = this.adminReturnUrl() ?? null;
        return { key: JSON.stringify({ mode, returnUrl, adminReturnUrl }), mode, returnUrl, adminReturnUrl };
    });

    public constructor() {
        effect(() => {
            if (!this.authService.isAuthReady() || !this.authService.isAuthenticated()) {
                return;
            }

            if (this.authDialogOpen || this.auth() !== undefined) {
                return;
            }

            void this.navigationService.navigateToHomeAsync();
        });

        effect(() => {
            const request = this.requestedAuth();
            if (request === null) {
                this.authRequest = null;
                return;
            }
            if (this.authRequest?.key === request.key) {
                return;
            }
            this.authRequest = request;
            untracked(() => {
                void this.openAuthDialogAsync(request);
            });
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
            canOpen: () => this.authRequest === request && this.requestedAuth()?.key === request.key,
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

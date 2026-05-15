import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { AuthService } from '../../../../services/auth.service';
import { resolveAppLocale } from '../../../../shared/lib/locale.constants';
import { PremiumBillingService } from '../../api/premium-billing.service';
import { PaddleCheckoutService } from '../../lib/paddle-checkout.service';
import type { BillingOverview, BillingPlan, BillingProvider } from '../../models/billing.models';
import { PremiumAccessBannersComponent } from '../premium-access-sections/access-banners/premium-access-banners.component';
import { PremiumBenefitsCardComponent } from '../premium-access-sections/benefits-card/premium-benefits-card.component';
import { PremiumOverviewCardComponent } from '../premium-access-sections/overview-card/premium-overview-card.component';
import { PremiumPlansCardComponent } from '../premium-access-sections/plans-card/premium-plans-card.component';
import type {
    PremiumCheckoutRequest,
    PremiumOverviewCardViewModel,
    PremiumPlanCardViewModel,
} from './premium-access-lib/premium-access.types';
import { formatPremiumMediumDate } from './premium-access-lib/premium-access-date.utils';
import { resolvePremiumErrorMessage } from './premium-access-lib/premium-access-error.utils';
import { buildPremiumOverviewCardViewModel, buildPremiumPlanCards } from './premium-access-lib/premium-access-view.mapper';

@Component({
    selector: 'fd-premium-access-page',
    templateUrl: './premium-access-page.component.html',
    styleUrls: ['./premium-access-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FdPageContainerDirective,
        PageHeaderComponent,
        TranslatePipe,
        PremiumAccessBannersComponent,
        PremiumOverviewCardComponent,
        PremiumPlansCardComponent,
        PremiumBenefitsCardComponent,
    ],
})
export class PremiumAccessPageComponent {
    private readonly billingService = inject(PremiumBillingService);
    private readonly paddleCheckoutService = inject(PaddleCheckoutService);
    private readonly authService = inject(AuthService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);

    private readonly isBrowser = isPlatformBrowser(this.platformId);

    public readonly overview = signal<BillingOverview | null>(null);
    public readonly isLoading = signal(true);
    public readonly checkoutLoadingPlan = signal<BillingPlan | null>(null);
    public readonly portalLoading = signal(false);
    public readonly errorMessage = signal<string | null>(null);
    public readonly checkoutReturnState = signal<'success' | 'canceled' | null>(null);
    private readonly languageVersion = signal(0);

    public readonly isPremium = computed(() => this.overview()?.isPremium ?? this.authService.isPremium());
    public readonly availableProviders = computed(() => {
        const overview = this.overview();
        return overview?.availableProviders.filter(provider => provider.trim().length > 0) ?? [];
    });
    public readonly checkoutAvailable = computed(() => this.availableProviders().length > 0);
    public readonly showPlans = computed(() => !this.isLoading() && !this.isPremium() && this.checkoutAvailable());
    public readonly currentPeriodEndLabel = computed(() => {
        this.languageVersion();
        return this.formatMediumDate(this.overview()?.currentPeriodEndUtc);
    });
    public readonly overviewViewModel = computed<PremiumOverviewCardViewModel>(() =>
        buildPremiumOverviewCardViewModel(this.overview(), this.isPremium(), this.checkoutAvailable(), this.currentPeriodEndLabel()),
    );
    public readonly planCards = computed<PremiumPlanCardViewModel[]>(() =>
        buildPremiumPlanCards(this.availableProviders(), this.checkoutLoadingPlan()),
    );

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
        void this.initializePageAsync();
    }

    public async startCheckoutAsync(plan: BillingPlan, provider?: BillingProvider): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        this.errorMessage.set(null);
        this.checkoutLoadingPlan.set(plan);

        try {
            const session = await firstValueFrom(this.billingService.createCheckoutSession(plan, provider));
            if (session.url.length === 0) {
                this.showErrorMessage('Checkout URL is missing.');
                return;
            }

            this.document.location.href = session.url;
        } catch (error) {
            this.showErrorMessage(this.getErrorMessage(error));
        } finally {
            this.checkoutLoadingPlan.set(null);
        }
    }

    public async startCheckoutFromViewAsync(request: PremiumCheckoutRequest): Promise<void> {
        await this.startCheckoutAsync(request.plan, request.provider);
    }

    public async openPortalAsync(): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        this.errorMessage.set(null);
        this.portalLoading.set(true);

        try {
            const session = await firstValueFrom(this.billingService.createPortalSession());
            if (session.url.length === 0) {
                this.showErrorMessage('Portal URL is missing.');
                return;
            }

            this.document.location.href = session.url;
        } catch (error) {
            this.showErrorMessage(this.getErrorMessage(error));
        } finally {
            this.portalLoading.set(false);
        }
    }

    public async reloadOverviewAsync(): Promise<void> {
        await this.loadOverviewAsync();
    }

    private formatMediumDate(value: string | null | undefined): string | null {
        return formatPremiumMediumDate(value, resolveAppLocale(this.translateService.getCurrentLang()));
    }

    private async initializePageAsync(): Promise<void> {
        await this.handleCheckoutReturnStateAsync();
        await this.loadOverviewAsync();
        await this.handlePaddleTransactionCheckoutAsync();
    }

    private async handleCheckoutReturnStateAsync(): Promise<void> {
        const checkoutState = this.route.snapshot.queryParamMap.get('checkout');
        if (checkoutState === null || checkoutState.length === 0) {
            return;
        }

        if (checkoutState === 'success') {
            await firstValueFrom(this.authService.refreshToken());
            this.checkoutReturnState.set('success');
            this.toastService.success(this.translateService.instant('PREMIUM_PAGE.BANNERS.CHECKOUT_SUCCESS_MESSAGE'));
        } else if (checkoutState === 'canceled') {
            this.checkoutReturnState.set('canceled');
            this.toastService.info(this.translateService.instant('PREMIUM_PAGE.BANNERS.CHECKOUT_CANCELED_MESSAGE'));
        }

        if (!this.isBrowser) {
            return;
        }

        await this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {},
            replaceUrl: true,
        });
    }

    private async loadOverviewAsync(): Promise<void> {
        this.isLoading.set(true);
        this.errorMessage.set(null);

        try {
            const overview = await firstValueFrom(this.billingService.getOverview());
            this.overview.set(overview);
        } catch (error) {
            this.errorMessage.set(this.getErrorMessage(error));
            this.overview.set(null);
        } finally {
            this.isLoading.set(false);
        }
    }

    private async handlePaddleTransactionCheckoutAsync(): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        const transactionId = this.route.snapshot.queryParamMap.get('_ptxn');
        if (transactionId === null || transactionId.length === 0) {
            return;
        }

        const paddleClientToken = this.resolvePaddleClientToken();
        if (paddleClientToken === null) {
            this.showPaddleClientTokenMissingError();
            return;
        }

        try {
            await this.openPaddleTransactionCheckoutAsync(transactionId, paddleClientToken);

            await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: {},
                replaceUrl: true,
            });
        } catch (error) {
            this.showErrorMessage(this.getErrorMessage(error));
        }
    }

    private resolvePaddleClientToken(): string | null {
        const token = this.overview()?.paddleClientToken?.trim() ?? environment.paddleClientToken?.trim();
        return token !== undefined && token.length > 0 ? token : null;
    }

    private showPaddleClientTokenMissingError(): void {
        this.showErrorMessage('Paddle client token is not configured.');
    }

    private showErrorMessage(message: string): void {
        this.errorMessage.set(message);
        this.toastService.error(message);
    }

    private async openPaddleTransactionCheckoutAsync(transactionId: string, paddleClientToken: string): Promise<void> {
        await this.paddleCheckoutService.openTransactionCheckoutAsync(transactionId, {
            token: paddleClientToken,
            environment: paddleClientToken.startsWith('test_') ? 'sandbox' : 'production',
            locale: this.resolveCheckoutLocale(),
        });
    }

    private getErrorMessage(error: unknown): string {
        return resolvePremiumErrorMessage(error, this.translateService.instant('PREMIUM_PAGE.ERROR_GENERIC'));
    }

    private resolveCheckoutLocale(): string {
        const currentLang = this.translateService.getCurrentLang();
        if (currentLang.length > 0) {
            return currentLang;
        }

        const fallbackLang = this.translateService.getFallbackLang() ?? '';
        return fallbackLang.length > 0 ? fallbackLang : 'en';
    }
}

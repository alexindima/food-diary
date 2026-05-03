import { CommonModule, DatePipe, DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, PLATFORM_ID, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { NoticeBannerComponent } from '../../../components/shared/notice-banner/notice-banner.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { AuthService } from '../../../services/auth.service';
import { PremiumBillingService } from '../api/premium-billing.service';
import { PaddleCheckoutService } from '../lib/paddle-checkout.service';
import { type BillingOverview, type BillingPlan, type BillingProvider } from '../models/billing.models';

@Component({
    selector: 'fd-premium-access-page',
    standalone: true,
    templateUrl: './premium-access-page.component.html',
    styleUrls: ['./premium-access-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        DatePipe,
        FdPageContainerDirective,
        PageHeaderComponent,
        FdUiButtonComponent,
        FdUiCardComponent,
        NoticeBannerComponent,
        TranslatePipe,
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
    private readonly document = inject(DOCUMENT);
    private readonly platformId = inject(PLATFORM_ID);

    private readonly isBrowser = isPlatformBrowser(this.platformId);

    public readonly overview = signal<BillingOverview | null>(null);
    public readonly isLoading = signal(true);
    public readonly checkoutLoadingPlan = signal<BillingPlan | null>(null);
    public readonly portalLoading = signal(false);
    public readonly errorMessage = signal<string | null>(null);
    public readonly checkoutReturnState = signal<'success' | 'canceled' | null>(null);

    public readonly canManageBilling = computed(() => this.overview()?.manageBillingAvailable ?? false);
    public readonly isPremium = computed(() => this.overview()?.isPremium ?? this.authService.isPremium());
    public readonly showManageBilling = computed(() => this.canManageBilling() && this.isPremium());
    public readonly availableProviders = computed(() => {
        const overview = this.overview();
        const providers = overview?.availableProviders?.filter(provider => !!provider?.trim()) ?? [];
        return providers;
    });
    public readonly checkoutAvailable = computed(() => this.availableProviders().length > 0);
    public readonly showPlans = computed(() => !this.isLoading() && !this.isPremium() && this.checkoutAvailable());
    public readonly showProviderChoices = computed(() => this.availableProviders().length > 1);
    public readonly overviewHintKey = computed(() => {
        if (this.showManageBilling()) {
            return 'PREMIUM_PAGE.OVERVIEW.MANAGE_HINT';
        }

        return this.checkoutAvailable() ? 'PREMIUM_PAGE.OVERVIEW.CHECKOUT_HINT' : 'PREMIUM_PAGE.OVERVIEW.CHECKOUT_UNAVAILABLE_HINT';
    });

    public constructor() {
        void this.initializePage();
    }

    public async startCheckout(plan: BillingPlan, provider?: BillingProvider): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        this.errorMessage.set(null);
        this.checkoutLoadingPlan.set(plan);

        try {
            const session = await firstValueFrom(this.billingService.createCheckoutSession(plan, provider));
            if (!session.url) {
                throw new Error('Checkout URL is missing.');
            }

            this.document.location.href = session.url;
        } catch (error) {
            const message = this.getErrorMessage(error);
            this.errorMessage.set(message);
            this.toastService.error(message);
        } finally {
            this.checkoutLoadingPlan.set(null);
        }
    }

    public async openPortal(): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        this.errorMessage.set(null);
        this.portalLoading.set(true);

        try {
            const session = await firstValueFrom(this.billingService.createPortalSession());
            if (!session.url) {
                throw new Error('Portal URL is missing.');
            }

            this.document.location.href = session.url;
        } catch (error) {
            const message = this.getErrorMessage(error);
            this.errorMessage.set(message);
            this.toastService.error(message);
        } finally {
            this.portalLoading.set(false);
        }
    }

    public async reloadOverview(): Promise<void> {
        await this.loadOverview();
    }

    public getPlanLabelKey(plan: BillingPlan | null): string {
        return plan === 'yearly' ? 'PREMIUM_PAGE.PLANS.YEARLY.TITLE' : 'PREMIUM_PAGE.PLANS.MONTHLY.TITLE';
    }

    public getStatusLabelKey(status: string | null): string {
        switch (status) {
            case 'active':
                return 'PREMIUM_PAGE.STATUS.ACTIVE';
            case 'trialing':
                return 'PREMIUM_PAGE.STATUS.TRIALING';
            case 'past_due':
                return 'PREMIUM_PAGE.STATUS.PAST_DUE';
            case 'canceled':
                return 'PREMIUM_PAGE.STATUS.CANCELED';
            case 'unpaid':
                return 'PREMIUM_PAGE.STATUS.UNPAID';
            case 'incomplete':
                return 'PREMIUM_PAGE.STATUS.INCOMPLETE';
            default:
                return 'PREMIUM_PAGE.STATUS.NONE';
        }
    }

    public isPlanLoading(plan: BillingPlan): boolean {
        return this.checkoutLoadingPlan() === plan;
    }

    public getProviderLabel(provider: BillingProvider): string {
        switch (provider.toLowerCase()) {
            case 'yookassa':
                return 'YooKassa';
            case 'paddle':
                return 'Paddle';
            case 'stripe':
                return 'Stripe';
            default:
                return provider;
        }
    }

    private async initializePage(): Promise<void> {
        await this.handleCheckoutReturnState();
        await this.loadOverview();
        await this.handlePaddleTransactionCheckout();
    }

    private async handleCheckoutReturnState(): Promise<void> {
        const checkoutState = this.route.snapshot.queryParamMap.get('checkout');
        if (!checkoutState) {
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

    private async loadOverview(): Promise<void> {
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

    private async handlePaddleTransactionCheckout(): Promise<void> {
        if (!this.isBrowser) {
            return;
        }

        const transactionId = this.route.snapshot.queryParamMap.get('_ptxn');
        if (!transactionId) {
            return;
        }

        const paddleClientToken = this.overview()?.paddleClientToken?.trim() || environment.paddleClientToken?.trim();
        if (!paddleClientToken) {
            const message = 'Paddle client token is not configured.';
            this.errorMessage.set(message);
            this.toastService.error(message);
            return;
        }

        try {
            await this.paddleCheckoutService.openTransactionCheckout(transactionId, {
                token: paddleClientToken,
                environment: paddleClientToken.startsWith('test_') ? 'sandbox' : 'production',
                locale: this.translateService.currentLang || this.translateService.getDefaultLang() || 'en',
            });

            await this.router.navigate([], {
                relativeTo: this.route,
                queryParams: {},
                replaceUrl: true,
            });
        } catch (error) {
            const message = this.getErrorMessage(error);
            this.errorMessage.set(message);
            this.toastService.error(message);
        }
    }

    private getErrorMessage(error: unknown): string {
        if (error instanceof HttpErrorResponse) {
            const payload = error.error as { message?: string } | string | null;
            if (payload && typeof payload === 'object' && typeof payload.message === 'string' && payload.message.trim()) {
                return payload.message.trim();
            }

            if (typeof payload === 'string' && payload.trim()) {
                return payload.trim();
            }
        }

        if (error instanceof Error && error.message.trim()) {
            return error.message.trim();
        }

        return this.translateService.instant('PREMIUM_PAGE.ERROR_GENERIC');
    }
}

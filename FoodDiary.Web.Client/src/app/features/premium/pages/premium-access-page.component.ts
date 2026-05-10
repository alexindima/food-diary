import { CommonModule, DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import type { BillingOverview, BillingPlan, BillingProvider } from '../models/billing.models';

@Component({
    selector: 'fd-premium-access-page',
    standalone: true,
    templateUrl: './premium-access-page.component.html',
    styleUrls: ['./premium-access-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
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

    public readonly canManageBilling = computed(() => this.overview()?.manageBillingAvailable ?? false);
    public readonly isPremium = computed(() => this.overview()?.isPremium ?? this.authService.isPremium());
    public readonly showManageBilling = computed(() => this.canManageBilling() && this.isPremium());
    public readonly availableProviders = computed(() => {
        const overview = this.overview();
        const providers = overview?.availableProviders.filter(provider => !!provider.trim()) ?? [];
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
    public readonly overviewBadges = computed<PremiumOverviewBadgesViewModel>(() => {
        const overview = this.overview();

        return {
            planLabelKey: this.isPremium() && overview?.plan ? this.getPlanLabelKey(overview.plan) : null,
            statusLabelKey: this.getStatusLabelKey(overview?.subscriptionStatus ?? null),
        };
    });
    public readonly overviewCopyState = computed<PremiumOverviewCopyState>(() => {
        const overview = this.overview();

        return {
            stateLabelKey: this.isPremium() ? 'PREMIUM_PAGE.OVERVIEW.PREMIUM_STATE' : 'PREMIUM_PAGE.OVERVIEW.FREE_STATE',
            periodLabelKey: overview?.cancelAtPeriodEnd ? 'PREMIUM_PAGE.OVERVIEW.ENDS_ON' : 'PREMIUM_PAGE.OVERVIEW.RENEWS_ON',
            showCancelAtPeriodEndBanner: overview?.cancelAtPeriodEnd ?? false,
        };
    });
    public readonly currentPeriodEndLabel = computed(() => {
        this.languageVersion();
        return this.formatMediumDate(this.overview()?.currentPeriodEndUtc);
    });
    public readonly planCards = computed<PremiumPlanCardViewModel[]>(() => {
        const providers = this.availableProviders().map(provider => ({
            provider,
            label: this.getProviderLabel(provider),
        }));
        const loadingPlan = this.checkoutLoadingPlan();

        return [
            {
                plan: 'monthly' as const,
                titleKey: 'PREMIUM_PAGE.PLANS.MONTHLY.TITLE',
                descriptionKey: 'PREMIUM_PAGE.PLANS.MONTHLY.DESCRIPTION',
                actionKey: 'PREMIUM_PAGE.PLANS.MONTHLY.ACTION',
                isFeatured: false,
                kickerKey: null,
                isLoading: loadingPlan === 'monthly',
                providerOptions: providers,
            },
            {
                plan: 'yearly' as const,
                titleKey: 'PREMIUM_PAGE.PLANS.YEARLY.TITLE',
                descriptionKey: 'PREMIUM_PAGE.PLANS.YEARLY.DESCRIPTION',
                actionKey: 'PREMIUM_PAGE.PLANS.YEARLY.ACTION',
                isFeatured: true,
                kickerKey: 'PREMIUM_PAGE.PLANS.YEARLY.KICKER',
                isLoading: loadingPlan === 'yearly',
                providerOptions: providers,
            },
        ];
    });

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

    public async openPortalAsync(): Promise<void> {
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

    public async reloadOverviewAsync(): Promise<void> {
        await this.loadOverviewAsync();
    }

    private getPlanLabelKey(plan: BillingPlan | null): string {
        return plan === 'yearly' ? 'PREMIUM_PAGE.PLANS.YEARLY.TITLE' : 'PREMIUM_PAGE.PLANS.MONTHLY.TITLE';
    }

    private getStatusLabelKey(status: string | null): string {
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
            case null:
                return 'PREMIUM_PAGE.STATUS.NONE';
            default:
                return 'PREMIUM_PAGE.STATUS.NONE';
        }
    }

    private getProviderLabel(provider: BillingProvider): string {
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

    private formatMediumDate(value: string | null | undefined): string | null {
        if (!value) {
            return null;
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return null;
        }

        return new Intl.DateTimeFormat(this.translateService.currentLang === 'ru' ? 'ru-RU' : 'en-US', {
            dateStyle: 'medium',
        }).format(date);
    }

    private async initializePageAsync(): Promise<void> {
        await this.handleCheckoutReturnStateAsync();
        await this.loadOverviewAsync();
        await this.handlePaddleTransactionCheckoutAsync();
    }

    private async handleCheckoutReturnStateAsync(): Promise<void> {
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
        if (!transactionId) {
            return;
        }

        const paddleClientToken = this.overview()?.paddleClientToken?.trim() ?? environment.paddleClientToken?.trim();
        if (!paddleClientToken) {
            const message = 'Paddle client token is not configured.';
            this.errorMessage.set(message);
            this.toastService.error(message);
            return;
        }

        try {
            await this.paddleCheckoutService.openTransactionCheckoutAsync(transactionId, {
                token: paddleClientToken,
                environment: paddleClientToken.startsWith('test_') ? 'sandbox' : 'production',
                locale: (this.translateService.currentLang || this.translateService.getDefaultLang()) ?? 'en',
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

interface PremiumOverviewBadgesViewModel {
    planLabelKey: string | null;
    statusLabelKey: string;
}

interface PremiumOverviewCopyState {
    stateLabelKey: string;
    periodLabelKey: string;
    showCancelAtPeriodEndBanner: boolean;
}

interface PremiumPlanCardViewModel {
    plan: BillingPlan;
    titleKey: string;
    descriptionKey: string;
    actionKey: string;
    isFeatured: boolean;
    kickerKey: string | null;
    isLoading: boolean;
    providerOptions: PremiumProviderOptionViewModel[];
}

interface PremiumProviderOptionViewModel {
    provider: BillingProvider;
    label: string;
}

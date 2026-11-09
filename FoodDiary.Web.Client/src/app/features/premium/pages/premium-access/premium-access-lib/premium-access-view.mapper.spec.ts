import { describe, expect, it } from 'vitest';

import type { BillingOverview } from '../../../../../shared/models/billing.models';
import {
    buildPremiumOverviewBadges,
    buildPremiumOverviewCardViewModel,
    buildPremiumOverviewCopyState,
    buildPremiumPlanCards,
    getPremiumProviderLabel,
    getPremiumStatusLabelKey,
} from './premium-access-view.mapper';

const overview: BillingOverview = {
    isPremium: true,
    subscriptionStatus: 'active',
    plan: 'yearly',
    subscriptionProvider: 'paddle',
    currentPeriodStartUtc: '2026-05-01T00:00:00Z',
    currentPeriodEndUtc: '2026-06-01T00:00:00Z',
    nextBillingAttemptUtc: '2026-06-01T00:00:00Z',
    cancelAtPeriodEnd: false,
    renewalEnabled: true,
    manageBillingAvailable: true,
    premiumTrialStartUtc: null,
    premiumTrialEndUtc: null,
    premiumTrialActive: false,
    premiumTrialUsed: false,
    canStartPremiumTrial: false,
    provider: 'paddle',
    paddleClientToken: 'test_token',
    availableProviders: ['paddle'],
};

describe('premium access view mapper', () => {
    it('explains Stars renewal controls without requiring a provider portal', () => {
        const result = buildPremiumOverviewCardViewModel(
            { ...overview, subscriptionProvider: 'TelegramStars', manageBillingAvailable: false },
            true,
            true,
            { periodEnd: null, periodStart: null, nextBillingAttempt: null },
        );
        expect(result.hintKey).toBe('PREMIUM_PAGE.STARS.ACCESS_HINT');
    });

    it('builds overview badges for premium subscription', () => {
        expect(buildPremiumOverviewBadges(overview, true)).toEqual({
            planLabelKey: 'PREMIUM_PAGE.PLANS.YEARLY.TITLE',
            statusLabelKey: 'PREMIUM_PAGE.STATUS.ACTIVE',
        });
    });

    it('hides plan badge for free users', () => {
        expect(buildPremiumOverviewBadges({ ...overview, plan: 'monthly' }, false).planLabelKey).toBeNull();
    });

    it('builds canceled period copy state', () => {
        expect(buildPremiumOverviewCopyState({ ...overview, cancelAtPeriodEnd: true }, true)).toEqual({
            stateLabelKey: 'PREMIUM_PAGE.OVERVIEW.PREMIUM_STATE',
            periodLabelKey: 'PREMIUM_PAGE.OVERVIEW.ENDS_ON',
            showRenewalOffNotice: true,
        });
    });

    it('builds overview card model with manage hint', () => {
        expect(
            buildPremiumOverviewCardViewModel(overview, true, true, {
                periodEnd: 'Jun 1, 2026',
                periodStart: null,
                nextBillingAttempt: null,
            }),
        ).toMatchObject({
            copyState: {
                stateLabelKey: 'PREMIUM_PAGE.OVERVIEW.PREMIUM_STATE',
                periodLabelKey: 'PREMIUM_PAGE.OVERVIEW.RENEWS_ON',
                showRenewalOffNotice: false,
            },
            badges: {
                planLabelKey: 'PREMIUM_PAGE.PLANS.YEARLY.TITLE',
                statusLabelKey: 'PREMIUM_PAGE.STATUS.ACTIVE',
            },
            currentPeriodEndLabel: 'Jun 1, 2026',
            hintKey: 'PREMIUM_PAGE.OVERVIEW.MANAGE_HINT',
            showManageBilling: true,
            showStartTrial: false,
        });
    });

    it('builds overview card model with trial action', () => {
        const freeOverview = {
            ...overview,
            isPremium: false,
            subscriptionStatus: null,
            plan: null,
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            manageBillingAvailable: false,
            canStartPremiumTrial: true,
        };

        expect(
            buildPremiumOverviewCardViewModel(freeOverview, false, true, { periodEnd: null, periodStart: null, nextBillingAttempt: null }),
        ).toMatchObject({
            hintKey: 'PREMIUM_PAGE.OVERVIEW.TRIAL_HINT',
            showManageBilling: false,
            showStartTrial: true,
        });
    });

    it('explains why a used trial is unavailable even without trial dates on this diary', () => {
        expect(
            buildPremiumOverviewCardViewModel(
                { ...overview, isPremium: false, subscriptionStatus: null, manageBillingAvailable: false, premiumTrialUsed: true },
                false,
                true,
                { periodEnd: null, periodStart: null, nextBillingAttempt: null },
            ),
        ).toMatchObject({ hintKey: 'PREMIUM_PAGE.OVERVIEW.TRIAL_USED_HINT', showStartTrial: false });
    });
});

describe('premium plan choices', () => {
    it('builds plan cards with provider labels and loading state', () => {
        expect(buildPremiumPlanCards(['paddle', 'stripe'], 'yearly')).toMatchObject([
            {
                plan: 'monthly',
                isFeatured: false,
                isLoading: false,
                providerOptions: [
                    { provider: 'paddle', label: 'Paddle', actionKey: 'PREMIUM_PAGE.PLANS.PAY_WITH' },
                    { provider: 'stripe', label: 'Stripe', actionKey: 'PREMIUM_PAGE.PLANS.PAY_WITH' },
                ],
            },
            {
                plan: 'yearly',
                isFeatured: true,
                isLoading: true,
                providerOptions: [
                    { provider: 'paddle', label: 'Paddle', actionKey: 'PREMIUM_PAGE.PLANS.PAY_WITH' },
                    { provider: 'stripe', label: 'Stripe', actionKey: 'PREMIUM_PAGE.PLANS.PAY_WITH' },
                ],
            },
        ]);
    });

    it('falls back for unknown statuses and keeps unknown provider labels', () => {
        expect(getPremiumStatusLabelKey('unknown')).toBe('PREMIUM_PAGE.STATUS.NONE');
        expect(getPremiumProviderLabel('custom')).toBe('custom');
    });
});

describe('premium account entitlement', () => {
    it('keeps self-service management available when payment issues remove premium access', () => {
        const result = buildPremiumOverviewCardViewModel({ ...overview, isPremium: false, subscriptionStatus: 'unpaid' }, false, true, {
            periodEnd: null,
            periodStart: null,
            nextBillingAttempt: null,
        });

        expect(result.showManageBilling).toBe(true);
        expect(result.showStartTrial).toBe(false);
    });

    it('describes non-renewing access by its end date', () => {
        const result = buildPremiumOverviewCopyState({ ...overview, renewalEnabled: false }, true);

        expect(result.periodLabelKey).toBe('PREMIUM_PAGE.OVERVIEW.ENDS_ON');
    });

    it('presents entitlement without a billing subscription as active access', () => {
        const result = buildPremiumOverviewCardViewModel(
            { ...overview, subscriptionStatus: null, plan: null, manageBillingAvailable: false },
            true,
            true,
            { periodEnd: null, periodStart: null, nextBillingAttempt: null },
        );
        expect(result.badges).toEqual({ planLabelKey: null, statusLabelKey: 'PREMIUM_PAGE.STATUS.ACCESS_ACTIVE' });
        expect(result.hintKey).toBe('PREMIUM_PAGE.OVERVIEW.ACCESS_HINT');
        expect(result.showStartTrial).toBe(false);
        expect(result.showManageBilling).toBe(false);
    });
});

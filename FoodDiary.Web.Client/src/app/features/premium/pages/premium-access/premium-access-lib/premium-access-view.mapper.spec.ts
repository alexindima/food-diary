import { describe, expect, it } from 'vitest';

import type { BillingOverview } from '../../../models/billing.models';
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
    provider: 'paddle',
    paddleClientToken: 'test_token',
    availableProviders: ['paddle'],
};

describe('premium access view mapper', () => {
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
            showCancelAtPeriodEndBanner: true,
        });
    });

    it('builds overview card model with manage hint', () => {
        expect(buildPremiumOverviewCardViewModel(overview, true, true, 'Jun 1, 2026')).toEqual({
            copyState: {
                stateLabelKey: 'PREMIUM_PAGE.OVERVIEW.PREMIUM_STATE',
                periodLabelKey: 'PREMIUM_PAGE.OVERVIEW.RENEWS_ON',
                showCancelAtPeriodEndBanner: false,
            },
            badges: {
                planLabelKey: 'PREMIUM_PAGE.PLANS.YEARLY.TITLE',
                statusLabelKey: 'PREMIUM_PAGE.STATUS.ACTIVE',
            },
            currentPeriodEndLabel: 'Jun 1, 2026',
            hintKey: 'PREMIUM_PAGE.OVERVIEW.MANAGE_HINT',
            showManageBilling: true,
        });
    });

    it('builds plan cards with provider labels and loading state', () => {
        expect(buildPremiumPlanCards(['paddle', 'stripe'], 'yearly')).toMatchObject([
            {
                plan: 'monthly',
                isFeatured: false,
                isLoading: false,
                providerOptions: [
                    { provider: 'paddle', label: 'Paddle' },
                    { provider: 'stripe', label: 'Stripe' },
                ],
            },
            {
                plan: 'yearly',
                isFeatured: true,
                isLoading: true,
                providerOptions: [
                    { provider: 'paddle', label: 'Paddle' },
                    { provider: 'stripe', label: 'Stripe' },
                ],
            },
        ]);
    });

    it('falls back for unknown statuses and keeps unknown provider labels', () => {
        expect(getPremiumStatusLabelKey('unknown')).toBe('PREMIUM_PAGE.STATUS.NONE');
        expect(getPremiumProviderLabel('custom')).toBe('custom');
    });
});

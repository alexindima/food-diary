export type DashboardRange = { from?: string; to?: string; allTime?: boolean };
export type DashboardCurrency = {
    currency: string;
    gross: number;
    net: number;
    refunds: number;
    chargebacks: number;
    successfulPayments: number;
};
export type DashboardTrend = { date: string; registrations: number; aiTokens: number; revenue: Array<{ currency: string; gross: number }> };
export type DashboardPeriod = {
    fromUtc: string;
    toUtc: string;
    metrics: { registrations: number; payingUsers: number; aiTokens: number; trend: DashboardTrend[] };
    currencies: DashboardCurrency[];
};
export type AdminDashboardOverview = {
    fromUtc: string;
    toUtc: string;
    interval: 'day' | 'month';
    period: DashboardPeriod;
    previous: DashboardPeriod | null;
    totalUsersNow: number;
    premiumUsersNow: number;
    pendingReportsNow: number;
};

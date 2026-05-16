export type DashboardSummaryCardConfig = {
    ring: {
        outerRadius: number;
        innerRadius: number;
    };
    randomId: {
        radix: number;
        start: number;
        end: number;
    };
    gradient: {
        startWhiteMix: number;
        endWhiteMix: number;
    };
    animation: {
        msPerPercent: number;
    };
};

export const DEFAULT_DASHBOARD_SUMMARY_CARD_CONFIG: DashboardSummaryCardConfig = {
    ring: {
        outerRadius: 112,
        innerRadius: 88,
    },
    randomId: {
        radix: 36,
        start: 2,
        end: 9,
    },
    gradient: {
        startWhiteMix: 0.05,
        endWhiteMix: 0.15,
    },
    animation: {
        msPerPercent: 10,
    },
};

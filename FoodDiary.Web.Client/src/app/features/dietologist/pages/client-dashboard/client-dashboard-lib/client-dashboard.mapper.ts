import type { ClientSummary } from '../../../../../shared/models/dietologist.data';

export type ClientDashboardSection = {
    isVisible: boolean;
    titleKey: string;
    bodyKey: string;
};

export function getClientDashboardTitle(client: ClientSummary): string {
    const fullName = `${client.firstName ?? ''} ${client.lastName ?? ''}`.trim();
    return fullName.length > 0 ? fullName : client.email;
}

export function buildClientProfileChips(client: ClientSummary | null): string[] {
    if (client?.permissions.shareProfile !== true) {
        return [];
    }

    return [formatProfileChip(client.height, ' cm'), client.gender, client.activityLevel].filter(
        (value): value is string => value !== null && value.length > 0,
    );
}

export function buildClientDashboardSections(client: ClientSummary | null): ClientDashboardSection[] {
    const permissions = client?.permissions;
    if (permissions === undefined) {
        return [];
    }

    return [
        {
            isVisible: permissions.shareProfile,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.PROFILE_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.PROFILE_BODY',
        },
        {
            isVisible: permissions.shareStatistics,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.STATISTICS_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.STATISTICS_BODY',
        },
        {
            isVisible: permissions.shareMeals,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.MEALS_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.MEALS_BODY',
        },
        {
            isVisible: permissions.shareWeight,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WEIGHT_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WEIGHT_BODY',
        },
        {
            isVisible: permissions.shareWaist,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WAIST_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.WAIST_BODY',
        },
        {
            isVisible: permissions.shareGoals,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.GOALS_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.GOALS_BODY',
        },
        {
            isVisible: permissions.shareHydration,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.HYDRATION_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.HYDRATION_BODY',
        },
        {
            isVisible: permissions.shareFasting,
            titleKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.FASTING_TITLE',
            bodyKey: 'DIETOLOGIST.CLIENT_DASHBOARD.SECTIONS.FASTING_BODY',
        },
    ].filter(section => section.isVisible);
}

function formatProfileChip(value: number | null | undefined, suffix: string): string | null {
    return value === null || value === undefined ? null : `${value}${suffix}`;
}

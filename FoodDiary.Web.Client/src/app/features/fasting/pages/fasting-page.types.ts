import type { FdUiInlineAlertSeverity } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert.component';

import type { FastingCheckIn, FastingMessage, FastingSession, FastingStats } from '../models/fasting.data';

export interface FastingStatsViewModel {
    stats: FastingStats;
    hasPersonalSummary: boolean;
    topSymptomLabel: string;
}

export interface FastingMessageViewModel {
    message: FastingMessage;
    severity: FdUiInlineAlertSeverity;
    title: string;
    body: string;
}

export interface FastingCheckInViewModel {
    checkIn: FastingCheckIn;
    checkedInAtLabel: string;
    relativeCheckedInAt: string | null;
    summary: string;
    symptomLabels: string[];
}

export interface FastingHistorySessionViewModel {
    session: FastingSession;
    startedAtLabel: string;
    accentColor: string;
    sessionTypeLabel: string;
    protocolDisplay: string;
    badgeKey: string;
    hasCheckIns: boolean;
    checkInCount: number;
    canViewChart: boolean;
    isExpanded: boolean;
    checkInRegionId: string;
    toggleKey: string;
    visibleCheckIns: FastingCheckInViewModel[];
    canLoadMoreCheckIns: boolean;
}

import type { FdUiInlineAlertSeverity } from 'fd-ui-kit/inline-alert/fd-ui-inline-alert.component';

import type { FastingCheckIn, FastingMessage, FastingSession } from '../../models/fasting.data';

export type FastingMessageViewModel = {
    message: FastingMessage;
    severity: FdUiInlineAlertSeverity;
    title: string;
    body: string;
};

export type FastingCheckInViewModel = {
    checkIn: FastingCheckIn;
    checkedInAtLabel: string;
    relativeCheckedInAt: string | null;
    summary: string;
    symptomLabels: string[];
};

export type FastingHistorySessionViewModel = {
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
};

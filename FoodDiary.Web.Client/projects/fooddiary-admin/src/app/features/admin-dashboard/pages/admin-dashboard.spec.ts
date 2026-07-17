import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AdminAiUsageService } from '../../admin-ai-usage/api/admin-ai-usage.service';
import { AdminUsersFacade } from '../../admin-users/lib/admin-users.facade';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminTelemetryService } from '../api/admin-telemetry.service';
import { AdminDashboardFacade } from '../lib/admin-dashboard.facade';
import { AdminDashboardComponent } from './admin-dashboard';

const TOTAL_USERS = 10;
const ACTIVE_USERS = 8;
const PREMIUM_USERS = 3;
const DELETED_USERS = 2;
const TOTAL_TOKENS = 1000;
const INPUT_TOKENS = 600;
const OUTPUT_TOKENS = 400;
const FASTING_WINDOW_HOURS = 24;
const STARTED_SESSIONS = 12;
const COMPLETED_SESSIONS = 8;
const SAVED_CHECK_INS = 5;
const REMINDER_PRESET_SELECTIONS = 6;
const REMINDER_TIMING_SAVES = 3;
const PRESET_REMINDER_TIMING_SAVES = 2;
const MANUAL_REMINDER_TIMING_SAVES = 1;
const COMPLETION_RATE_PERCENT = 66.7;
const CHECK_IN_RATE_PERCENT = 41.7;
const AVERAGE_COMPLETED_DURATION_HOURS = 18.2;

type DashboardServicesMock = {
    getAiUsageSummary: ReturnType<typeof vi.fn>;
    getFastingSummary: ReturnType<typeof vi.fn>;
    getLoginSummary: ReturnType<typeof vi.fn>;
    getSummary: ReturnType<typeof vi.fn>;
};
type DashboardTestContext = {
    dashboard: AdminDashboardFacade;
    services: DashboardServicesMock;
    component: AdminDashboardComponent;
    fixture: ComponentFixture<AdminDashboardComponent>;
};

async function setupDashboardAsync(): Promise<DashboardTestContext> {
    const services: DashboardServicesMock = {
        getAiUsageSummary: vi.fn().mockReturnValue(of(createAiUsageSummary())),
        getFastingSummary: vi.fn().mockReturnValue(of(createFastingSummary())),
        getLoginSummary: vi.fn().mockReturnValue(of(createLoginSummary())),
        getSummary: vi.fn().mockReturnValue(of(createDashboardSummary())),
    };

    await TestBed.configureTestingModule({
        imports: [AdminDashboardComponent],
        providers: [
            AdminDashboardFacade,
            { provide: AdminDashboardService, useValue: { getSummary: services.getSummary } },
            { provide: AdminAiUsageService, useValue: { getSummary: services.getAiUsageSummary } },
            { provide: AdminTelemetryService, useValue: { getFastingSummary: services.getFastingSummary } },
            { provide: AdminUsersFacade, useValue: { getLoginSummary: services.getLoginSummary } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminDashboardComponent);
    const component = fixture.componentInstance;
    const dashboard = TestBed.inject(AdminDashboardFacade);
    fixture.detectChanges();

    return { component, dashboard, services, fixture };
}

describe('AdminDashboardComponent', () => {
    it('should create', async () => {
        const { component } = await setupDashboardAsync();

        expect(component).toBeTruthy();
    });

    it('should load dashboard summary, ai usage, fasting telemetry, and login summary on init', async () => {
        const { dashboard, services } = await setupDashboardAsync();

        expect(services.getSummary).toHaveBeenCalledTimes(1);
        expect(services.getAiUsageSummary).toHaveBeenCalledTimes(1);
        expect(services.getFastingSummary).toHaveBeenCalledTimes(1);
        expect(services.getLoginSummary).toHaveBeenCalledTimes(1);
        expect(dashboard.summary()?.totalUsers).toBe(TOTAL_USERS);
        expect(dashboard.aiUsage()?.totalTokens).toBe(TOTAL_TOKENS);
        expect(dashboard.fastingTelemetry()?.startedSessions).toBe(STARTED_SESSIONS);
        expect(dashboard.loginDeviceSegments()).toEqual([{ label: 'Desktop', value: 21 }]);
        expect(dashboard.loginOperatingSystemSegments()).toEqual([{ label: 'Windows', value: 21 }]);
        expect(dashboard.loginBrowserSegments()).toEqual([
            { label: 'Opera', value: 19 },
            { label: 'Chrome', value: 2 },
        ]);
        expect(dashboard.isLoading()).toBe(false);
    });

    it('should reset summary and loading state on dashboard error', async () => {
        const { dashboard, services } = await setupDashboardAsync();
        services.getSummary.mockReturnValueOnce(throwError(() => new Error('dashboard failed')));
        services.getAiUsageSummary.mockReturnValueOnce(of(null));
        services.getFastingSummary.mockReturnValueOnce(of(null));

        dashboard.load();

        expect(dashboard.summary()).toBeNull();
        expect(dashboard.isLoading()).toBe(false);
    });
});

function createDashboardSummary(): {
    totalUsers: number;
    activeUsers: number;
    premiumUsers: number;
    deletedUsers: number;
    recentUsers: never[];
} {
    return {
        totalUsers: TOTAL_USERS,
        activeUsers: ACTIVE_USERS,
        premiumUsers: PREMIUM_USERS,
        deletedUsers: DELETED_USERS,
        recentUsers: [],
    };
}

function createAiUsageSummary(): {
    totalTokens: number;
    inputTokens: number;
    outputTokens: number;
    byDay: never[];
    byOperation: never[];
    byModel: never[];
    byUser: never[];
} {
    return {
        totalTokens: TOTAL_TOKENS,
        inputTokens: INPUT_TOKENS,
        outputTokens: OUTPUT_TOKENS,
        byDay: [],
        byOperation: [],
        byModel: [],
        byUser: [],
    };
}

function createFastingSummary(): {
    windowHours: number;
    generatedAtUtc: string;
    startedSessions: number;
    completedSessions: number;
    savedCheckIns: number;
    reminderPresetSelections: number;
    reminderTimingSaves: number;
    presetReminderTimingSaves: number;
    manualReminderTimingSaves: number;
    completionRatePercent: number;
    checkInRatePercent: number;
    averageCompletedDurationHours: number;
    lastCheckInAtUtc: string;
    lastEventAtUtc: string;
    topPresets: never[];
} {
    return {
        windowHours: FASTING_WINDOW_HOURS,
        generatedAtUtc: '2026-04-12T10:00:00Z',
        startedSessions: STARTED_SESSIONS,
        completedSessions: COMPLETED_SESSIONS,
        savedCheckIns: SAVED_CHECK_INS,
        reminderPresetSelections: REMINDER_PRESET_SELECTIONS,
        reminderTimingSaves: REMINDER_TIMING_SAVES,
        presetReminderTimingSaves: PRESET_REMINDER_TIMING_SAVES,
        manualReminderTimingSaves: MANUAL_REMINDER_TIMING_SAVES,
        completionRatePercent: COMPLETION_RATE_PERCENT,
        checkInRatePercent: CHECK_IN_RATE_PERCENT,
        averageCompletedDurationHours: AVERAGE_COMPLETED_DURATION_HOURS,
        lastCheckInAtUtc: '2026-04-12T09:30:00Z',
        lastEventAtUtc: '2026-04-12T09:40:00Z',
        topPresets: [],
    };
}

function createLoginSummary(): Array<{ key: string; count: number; lastSeenAtUtc: string }> {
    return [
        { key: 'device:Desktop', count: 21, lastSeenAtUtc: '2026-05-23T20:29:10Z' },
        { key: 'os:Windows', count: 21, lastSeenAtUtc: '2026-05-23T20:29:10Z' },
        { key: 'browser:Opera', count: 19, lastSeenAtUtc: '2026-05-23T20:29:10Z' },
        { key: 'browser:Chrome', count: 2, lastSeenAtUtc: '2026-05-23T18:53:14Z' },
    ];
}

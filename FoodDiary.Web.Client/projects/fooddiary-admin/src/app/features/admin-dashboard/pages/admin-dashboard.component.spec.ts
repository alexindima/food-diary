import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAiUsageService } from '../../admin-ai-usage/api/admin-ai-usage.service';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminTelemetryService } from '../api/admin-telemetry.service';
import { AdminDashboardComponent } from './admin-dashboard.component';

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

describe('AdminDashboardComponent', () => {
    let component: AdminDashboardComponent;
    let fixture: ComponentFixture<AdminDashboardComponent>;
    let dashboardService: { getSummary: ReturnType<typeof vi.fn> };
    let aiUsageService: { getSummary: ReturnType<typeof vi.fn> };
    let telemetryService: { getFastingSummary: ReturnType<typeof vi.fn> };

    beforeEach(async () => {
        dashboardService = { getSummary: vi.fn() };
        aiUsageService = { getSummary: vi.fn() };
        telemetryService = { getFastingSummary: vi.fn() };

        dashboardService.getSummary.mockReturnValue(
            of({
                totalUsers: TOTAL_USERS,
                activeUsers: ACTIVE_USERS,
                premiumUsers: PREMIUM_USERS,
                deletedUsers: DELETED_USERS,
                recentUsers: [],
            }),
        );
        aiUsageService.getSummary.mockReturnValue(
            of({
                totalTokens: TOTAL_TOKENS,
                inputTokens: INPUT_TOKENS,
                outputTokens: OUTPUT_TOKENS,
                byDay: [],
                byOperation: [],
                byModel: [],
                byUser: [],
            }),
        );
        telemetryService.getFastingSummary.mockReturnValue(
            of({
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
            }),
        );

        await TestBed.configureTestingModule({
            imports: [AdminDashboardComponent],
            providers: [
                { provide: AdminDashboardService, useValue: dashboardService },
                { provide: AdminAiUsageService, useValue: aiUsageService },
                { provide: AdminTelemetryService, useValue: telemetryService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(AdminDashboardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load dashboard summary, ai usage, and fasting telemetry on init', () => {
        expect(dashboardService.getSummary).toHaveBeenCalledTimes(1);
        expect(aiUsageService.getSummary).toHaveBeenCalledTimes(1);
        expect(telemetryService.getFastingSummary).toHaveBeenCalledTimes(1);
        expect(component.summary()?.totalUsers).toBe(TOTAL_USERS);
        expect(component.aiUsage()?.totalTokens).toBe(TOTAL_TOKENS);
        expect(component.fastingTelemetry()?.startedSessions).toBe(STARTED_SESSIONS);
        expect(component.isLoading()).toBe(false);
    });

    it('should reset summary and loading state on dashboard error', async () => {
        dashboardService.getSummary.mockReturnValueOnce(throwError(() => new Error('dashboard failed')));
        aiUsageService.getSummary.mockReturnValueOnce(of(null));
        telemetryService.getFastingSummary.mockReturnValueOnce(of(null));

        fixture = TestBed.createComponent(AdminDashboardComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        await fixture.whenStable();

        expect(component.summary()).toBeNull();
        expect(component.isLoading()).toBe(false);
    });
});

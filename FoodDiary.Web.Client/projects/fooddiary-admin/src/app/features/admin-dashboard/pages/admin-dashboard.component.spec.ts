import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AdminAiUsageService } from '../../admin-ai-usage/api/admin-ai-usage.service';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminTelemetryService } from '../api/admin-telemetry.service';
import { AdminDashboardComponent } from './admin-dashboard.component';

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
                totalUsers: 10,
                activeUsers: 8,
                premiumUsers: 3,
                deletedUsers: 2,
                recentUsers: [],
            }),
        );
        aiUsageService.getSummary.mockReturnValue(
            of({
                totalTokens: 1000,
                inputTokens: 600,
                outputTokens: 400,
                byDay: [],
                byOperation: [],
                byModel: [],
                byUser: [],
            }),
        );
        telemetryService.getFastingSummary.mockReturnValue(
            of({
                windowHours: 24,
                generatedAtUtc: '2026-04-12T10:00:00Z',
                startedSessions: 12,
                completedSessions: 8,
                savedCheckIns: 5,
                reminderPresetSelections: 6,
                reminderTimingSaves: 3,
                presetReminderTimingSaves: 2,
                manualReminderTimingSaves: 1,
                completionRatePercent: 66.7,
                checkInRatePercent: 41.7,
                averageCompletedDurationHours: 18.2,
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
        expect(component.summary()?.totalUsers).toBe(10);
        expect(component.aiUsage()?.totalTokens).toBe(1000);
        expect(component.fastingTelemetry()?.startedSessions).toBe(12);
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

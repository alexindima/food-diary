import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AdminTelemetryService } from './admin-telemetry.service';

describe('AdminTelemetryService', () => {
    let service: AdminTelemetryService;
    let httpMock: HttpTestingController;

    const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/telemetry/fasting`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminTelemetryService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminTelemetryService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should request fasting telemetry summary', () => {
        const response = {
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
        };

        service.getFastingSummary().subscribe(result => {
            expect(result).toEqual(response);
        });

        const req = httpMock.expectOne(`${baseUrl}?hours=24`);
        expect(req.request.method).toBe('GET');
        req.flush(response);
    });

    it('keeps observed zero counts and represents missing reminder timing and activity as null', () => {
        const counts = {
            startedSessions: 0,
            completedSessions: 0,
            savedCheckIns: 0,
            completionRatePercent: 0,
            checkInRatePercent: 0,
        };
        const reminder = { ...counts, presetId: '16-8', selectionCount: 1, timingSaveCount: 0 };
        const configured = { ...reminder, presetId: '14-10', firstReminderHours: 12, followUpReminderHours: 2 };
        const response = {
            ...counts,
            windowHours: 48,
            generatedAtUtc: '2026-04-12T10:00:00Z',
            reminderPresetSelections: 2,
            reminderTimingSaves: 0,
            presetReminderTimingSaves: 0,
            manualReminderTimingSaves: 0,
            topPresets: [reminder, configured],
        };
        const receive = vi.fn();
        service.getFastingSummary(response.windowHours).subscribe(receive);

        httpMock.expectOne(`${baseUrl}?hours=${response.windowHours}`).flush(response);

        expect(receive).toHaveBeenCalledExactlyOnceWith({
            ...response,
            averageCompletedDurationHours: null,
            lastCheckInAtUtc: null,
            lastEventAtUtc: null,
            topPresets: [{ ...reminder, firstReminderHours: null, followUpReminderHours: null }, configured],
        });
    });
});

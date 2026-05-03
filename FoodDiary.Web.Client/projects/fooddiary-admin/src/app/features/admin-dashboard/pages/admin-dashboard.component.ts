import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { AdminAiUsageService } from '../../admin-ai-usage/api/admin-ai-usage.service';
import { type AdminAiUsageSummary } from '../../admin-ai-usage/models/admin-ai-usage.data';
import { AdminDashboardService } from '../api/admin-dashboard.service';
import { AdminTelemetryService } from '../api/admin-telemetry.service';
import { type AdminDashboardSummary } from '../models/admin-dashboard.data';
import { type FastingTelemetrySummary } from '../models/admin-telemetry.data';

@Component({
    selector: 'fd-admin-dashboard',
    standalone: true,
    imports: [CommonModule, FdUiCardComponent],
    templateUrl: './admin-dashboard.component.html',
    styleUrl: './admin-dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardComponent {
    private readonly dashboardService = inject(AdminDashboardService);
    private readonly aiUsageService = inject(AdminAiUsageService);
    private readonly telemetryService = inject(AdminTelemetryService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly summary = signal<AdminDashboardSummary | null>(null);
    public readonly aiUsage = signal<AdminAiUsageSummary | null>(null);
    public readonly fastingTelemetry = signal<FastingTelemetrySummary | null>(null);
    public readonly isLoading = signal(false);

    public constructor() {
        this.loadSummary();
    }

    public loadSummary(): void {
        this.isLoading.set(true);
        this.dashboardService
            .getSummary()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.summary.set(response);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.summary.set(null);
                    this.isLoading.set(false);
                },
            });

        this.aiUsageService
            .getSummary()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.aiUsage.set(response);
                },
                error: () => {
                    this.aiUsage.set(null);
                },
            });

        this.telemetryService
            .getFastingSummary()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: response => {
                    this.fastingTelemetry.set(response);
                },
                error: () => {
                    this.fastingTelemetry.set(null);
                },
            });
    }

    public formatMetric(value: number | null | undefined): string {
        if (value === null || value === undefined || Number.isNaN(value)) {
            return '-';
        }

        return Number.isInteger(value) ? `${value}` : value.toFixed(1);
    }

    public formatRelativeTime(value: string | null): string | null {
        if (!value) {
            return null;
        }

        const timestamp = new Date(value).getTime();
        if (Number.isNaN(timestamp)) {
            return null;
        }

        const diffMs = timestamp - Date.now();
        const diffMinutes = Math.round(diffMs / 60000);
        const formatter = new Intl.RelativeTimeFormat('en-US', { numeric: 'auto' });

        if (Math.abs(diffMinutes) < 60) {
            return formatter.format(diffMinutes, 'minute');
        }

        const diffHours = Math.round(diffMinutes / 60);
        if (Math.abs(diffHours) < 24) {
            return formatter.format(diffHours, 'hour');
        }

        const diffDays = Math.round(diffHours / 24);
        return formatter.format(diffDays, 'day');
    }
}

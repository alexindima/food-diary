import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FdUiCardComponent, FdUiPieChartComponent } from 'fd-ui-kit';

import { AdminDashboardFacade } from '../lib/admin-dashboard.facade';

@Component({
    selector: 'fd-admin-dashboard',
    imports: [CommonModule, FdUiCardComponent, FdUiPieChartComponent],
    templateUrl: './admin-dashboard.html',
    styleUrl: './admin-dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardComponent {
    private readonly dashboard = inject(AdminDashboardFacade);

    protected readonly summary = this.dashboard.summary;
    protected readonly aiUsage = this.dashboard.aiUsage;
    protected readonly loginDeviceSegments = this.dashboard.loginDeviceSegments;
    protected readonly loginOperatingSystemSegments = this.dashboard.loginOperatingSystemSegments;
    protected readonly loginBrowserSegments = this.dashboard.loginBrowserSegments;
    protected readonly fastingTelemetryView = this.dashboard.fastingTelemetryView;
    protected readonly isLoading = this.dashboard.isLoading;

    public constructor() {
        this.dashboard.load();
    }

    protected loadSummary(): void {
        this.dashboard.load();
    }
}

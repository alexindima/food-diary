import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDateInputComponent, FdUiSelectComponent } from 'fd-ui-kit';

import { validAdminDate } from '../../../shared/period/admin-period';
import { firstAdminQueryValue } from '../../../shared/period/admin-query';
import { AdminDashboardContentComponent } from '../components/admin-dashboard-content';
import { AdminDashboardFacade } from '../lib/admin-dashboard.facade';
import type { DashboardRange } from '../models/admin-dashboard-overview.data';

const DAY_MS = 86_400_000;
const DATE_LENGTH = 10;
const MAX_DAYS = 3660;
const PRESET_OFFSET = { today: 0, '7d': 6, '30d': 29, month: 0, all: 0, custom: 0 };
const PRESETS = ['today', '7d', '30d', 'month', 'all', 'custom'] as const;
type Preset = (typeof PRESETS)[number];
const isoDate = (date: Date): string => date.toISOString().slice(0, DATE_LENGTH);
const dashboardPreset = (value: string | null): Preset => PRESETS.find(item => item === value) ?? 'month';

@Component({
    selector: 'fd-admin-dashboard',
    imports: [
        CommonModule,

        TranslatePipe,
        AdminDashboardContentComponent,
        FdUiButtonComponent,
        FdUiSelectComponent,
        FdUiDateInputComponent,
    ],
    templateUrl: './admin-dashboard.html',
    styleUrl: './admin-dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardComponent {
    public readonly period = input(undefined, { transform: firstAdminQueryValue });
    public readonly from = input(undefined, { transform: firstAdminQueryValue });
    public readonly to = input(undefined, { transform: firstAdminQueryValue });

    private readonly dashboard = inject(AdminDashboardFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    protected readonly today = isoDate(new Date());
    private readonly appliedPeriod = computed(() => {
        const preset = dashboardPreset(this.period() ?? null);
        const from = this.from() ?? null;
        const to = this.to() ?? null;
        return { preset, from, to, range: this.range(preset, from, to) };
    });
    protected readonly allTime = computed(() => this.appliedPeriod().range?.allTime === true);
    protected readonly preset = linkedSignal(() => this.appliedPeriod().preset);
    protected readonly draftFrom = linkedSignal<string | Date | null>(() => this.appliedPeriod().from);
    protected readonly draftTo = linkedSignal<string | Date | null>(() => this.appliedPeriod().to);
    protected readonly invalid = linkedSignal(() => this.appliedPeriod().range === null);
    private readonly refreshRevision = signal(0);
    private readonly overviewResource = rxResource({
        params: () => {
            const range = this.appliedPeriod().range;
            return range === null ? undefined : { range, revision: this.refreshRevision() };
        },
        stream: ({ params }) => this.dashboard.getOverview(params.range),
    });
    protected readonly data = computed(() => (this.overviewResource.hasValue() ? this.overviewResource.value() : null));
    protected readonly isLoading = this.overviewResource.isLoading;
    protected readonly failed = computed(() => this.overviewResource.error() !== undefined);

    protected setPreset(value: string | null): void {
        const preset = dashboardPreset(value);
        this.preset.set(preset);
        if (preset !== 'custom') {
            this.apply();
        }
    }

    protected apply(): void {
        const preset = this.preset();
        const range = this.range(preset, this.draftFrom(), this.draftTo());
        this.invalid.set(range === null);
        if (range === null) {
            return;
        }
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {
                period: preset,
                from: preset === 'custom' ? range.from : null,
                to: preset === 'custom' ? range.to : null,
            },
            queryParamsHandling: 'merge',
        });
    }

    protected reload(): void {
        // Refresh replaces an in-flight request for the applied period.
        this.refreshRevision.update(revision => revision + 1);
    }

    private range(preset: Preset, draftFrom: string | Date | null, draftTo: string | Date | null): DashboardRange | null {
        if (preset === 'all') {
            return { allTime: true };
        }
        const today = new Date(`${this.today}T00:00:00Z`);
        if (preset === 'custom') {
            const from = this.dateValue(draftFrom);
            const to = this.dateValue(draftTo);
            if (
                from === null ||
                to === null ||
                from > to ||
                to > this.today ||
                from < '1970-01-01' ||
                (Date.parse(to) - Date.parse(from)) / DAY_MS > MAX_DAYS
            ) {
                return null;
            }
            return { from, to };
        }
        const start = new Date(today);
        if (preset === 'month') {
            start.setUTCDate(1);
        } else {
            start.setUTCDate(start.getUTCDate() - PRESET_OFFSET[preset]);
        }
        return { from: isoDate(start), to: this.today };
    }

    private dateValue(value: string | Date | null): string | null {
        const date =
            value instanceof Date
                ? `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
                : value;
        return validAdminDate(date) ? date : null;
    }
}

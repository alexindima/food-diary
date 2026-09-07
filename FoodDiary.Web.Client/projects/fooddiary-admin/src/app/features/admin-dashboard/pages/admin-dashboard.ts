import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDateInputComponent, FdUiSelectComponent } from 'fd-ui-kit';

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
    protected readonly dashboard = inject(AdminDashboardFacade);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly preset = signal<Preset>('month');
    protected readonly from = signal<string | Date | null>(null);
    protected readonly to = signal<string | Date | null>(null);
    protected readonly invalid = signal(false);
    protected readonly today = isoDate(new Date());
    protected readonly data = this.dashboard.overview;

    public constructor() {
        this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
            const preset = params.get('period');
            this.preset.set(PRESETS.find(item => item === preset) ?? 'month');
            this.from.set(params.get('from'));
            this.to.set(params.get('to'));
            this.reload();
        });
    }

    protected setPreset(value: string | null): void {
        this.preset.set(PRESETS.find(item => item === value) ?? 'month');
        if (value !== 'custom') {
            this.apply();
        }
    }

    protected apply(): void {
        if (this.range() === null) {
            return;
        }
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: {
                period: this.preset(),
                from: this.preset() === 'custom' ? this.dateValue(this.from()) : null,
                to: this.preset() === 'custom' ? this.dateValue(this.to()) : null,
            },
            queryParamsHandling: 'merge',
        });
    }

    protected reload(): void {
        const range = this.range();
        if (range !== null) {
            this.dashboard.load(range);
        } else {
            this.dashboard.clear();
        }
    }

    private range(): DashboardRange | null {
        this.invalid.set(false);
        if (this.preset() === 'all') {
            return { allTime: true };
        }
        const today = new Date(`${this.today}T00:00:00Z`);
        if (this.preset() === 'custom') {
            const from = this.dateValue(this.from());
            const to = this.dateValue(this.to());
            if (
                from === null ||
                to === null ||
                from > to ||
                to > this.today ||
                from < '1970-01-01' ||
                (Date.parse(to) - Date.parse(from)) / DAY_MS > MAX_DAYS
            ) {
                this.invalid.set(true);
                return null;
            }
            return { from, to };
        }
        const start = new Date(today);
        if (this.preset() === 'month') {
            start.setUTCDate(1);
        } else {
            start.setUTCDate(start.getUTCDate() - PRESET_OFFSET[this.preset()]);
        }
        return { from: isoDate(start), to: this.today };
    }

    private dateValue(value: string | Date | null): string | null {
        if (value instanceof Date) {
            return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
        }
        return value !== null && /^\d{4}-\d{2}-\d{2}$/.test(value) && Number.isFinite(Date.parse(value)) ? value : null;
    }
}

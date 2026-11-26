import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDateInputComponent, FdUiSelectComponent } from 'fd-ui-kit';

import { adminPeriod } from './admin-period';
import { ADMIN_DATE_TEXT_LENGTH } from './admin-query';

@Component({
    selector: 'fd-admin-period',
    host: { '[class.admin-period-inline]': 'inline()' },
    imports: [TranslatePipe, FdUiButtonComponent, FdUiDateInputComponent, FdUiSelectComponent],
    styleUrl: './admin-period-control.scss',
    templateUrl: './admin-period-control.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPeriodControlComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly translate = inject(TranslateService);
    private readonly language = toSignal(this.translate.onLangChange);
    public readonly period = input.required<string | undefined>();
    public readonly periodFrom = input.required<string | undefined>();
    public readonly periodTo = input.required<string | undefined>();
    public readonly queryPrefix = input('');
    public readonly inline = input(false);
    public readonly defaultPeriod = input('all');
    public readonly label = input('ADMIN_COMMON.PERIOD');
    private readonly urlPeriod = computed(() => {
        const defaultPeriod = this.defaultPeriod();
        return {
            defaultPeriod,
            period: this.period() ?? defaultPeriod,
            from: this.periodFrom() ?? null,
            to: this.periodTo() ?? null,
        };
    });
    protected readonly preset = linkedSignal(() => this.urlPeriod().period);
    protected readonly from = linkedSignal<string | Date | null>(() => this.urlPeriod().from);
    protected readonly to = linkedSignal<string | Date | null>(() => this.urlPeriod().to);
    protected readonly invalid = computed(() => {
        const { period, from, to, defaultPeriod } = this.urlPeriod();
        return adminPeriod(convertToParamMap({ period, from, to }), defaultPeriod) === null;
    });
    protected readonly today = new Date().toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH);
    protected readonly presets = computed(() => {
        this.language();
        return ['all', 'today', '7d', '30d', '90d', 'month', 'custom'].map(value => ({
            value,
            label: String(this.translate.instant(`ADMIN_COMMON.PERIOD_${value}`)),
        }));
    });

    protected select(value: string | null): void {
        this.preset.set(value ?? 'all');
        if (value !== 'custom') {
            this.apply();
        }
    }

    protected apply(): void {
        const queryParams = { period: this.preset(), from: this.date(this.from()), to: this.date(this.to()), page: null };
        if (this.preset() !== 'custom') {
            queryParams.from = null;
            queryParams.to = null;
        }
        const scopedParams = Object.fromEntries(
            Object.entries(queryParams).map(([key, value]) => [key === 'page' ? key : `${this.queryPrefix()}${key}`, value]),
        );
        void this.router.navigate([], { relativeTo: this.route, queryParams: scopedParams, queryParamsHandling: 'merge' });
    }

    private date(value: string | Date | null): string | null {
        return value instanceof Date
            ? `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
            : value;
    }
}

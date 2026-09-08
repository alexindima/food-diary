import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiDateInputComponent, FdUiSelectComponent } from 'fd-ui-kit';

import { adminPeriod } from './admin-period';
import { ADMIN_DATE_TEXT_LENGTH } from './admin-query';

@Component({
    selector: 'fd-admin-period',
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
    private readonly params = toSignal(this.route.queryParamMap, { requireSync: true });
    public readonly defaultPeriod = input('all');
    public readonly label = input('ADMIN_COMMON.PERIOD');
    protected readonly preset = signal('all');
    protected readonly from = signal<string | Date | null>(null);
    protected readonly to = signal<string | Date | null>(null);
    protected readonly invalid = signal(false);
    protected readonly today = new Date().toISOString().slice(0, ADMIN_DATE_TEXT_LENGTH);
    protected readonly presets = computed(() => {
        this.language();
        return ['all', 'today', '7d', '30d', 'month', 'custom'].map(value => ({
            value,
            label: String(this.translate.instant(`ADMIN_COMMON.PERIOD_${value}`)),
        }));
    });

    public constructor() {
        effect(() => {
            const params = this.params();
            this.preset.set(params.get('period') ?? this.defaultPeriod());
            this.from.set(params.get('from'));
            this.to.set(params.get('to'));
            this.invalid.set(adminPeriod(params, this.defaultPeriod()) === null);
        });
    }

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
        void this.router.navigate([], { relativeTo: this.route, queryParams, queryParamsHandling: 'merge' });
    }

    private date(value: string | Date | null): string | null {
        return value instanceof Date
            ? `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
            : value;
    }
}

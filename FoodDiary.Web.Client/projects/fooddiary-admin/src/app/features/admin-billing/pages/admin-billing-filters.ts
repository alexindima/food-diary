import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { fdUiCoerceInputTextValue, FdUiInputComponent, type FdUiInputValue } from 'fd-ui-kit/input/fd-ui-input';

import type { AdminBillingTab } from '../models/admin-billing.models';

@Component({
    selector: 'fd-admin-billing-filters',
    imports: [FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-billing-filters.html',
    styleUrl: './admin-billing.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminBillingFiltersComponent {
    public readonly activeTab = input.required<AdminBillingTab>();
    public readonly search = input.required<string>();
    public readonly provider = input.required<string>();
    public readonly status = input.required<string>();
    public readonly kind = input.required<string>();
    public readonly fromDate = input.required<string>();
    public readonly toDate = input.required<string>();

    public readonly searchChange = output<string>();
    public readonly providerChange = output<string>();
    public readonly statusChange = output<string>();
    public readonly kindChange = output<string>();
    public readonly fromDateChange = output<string>();
    public readonly toDateChange = output<string>();
    public readonly filtersApply = output();
    public readonly filtersReset = output();

    protected getInputValue(event: Event): string {
        return event.target instanceof HTMLInputElement ? event.target.value : '';
    }

    protected getControlTextValue(value: FdUiInputValue): string {
        return fdUiCoerceInputTextValue(value);
    }
}

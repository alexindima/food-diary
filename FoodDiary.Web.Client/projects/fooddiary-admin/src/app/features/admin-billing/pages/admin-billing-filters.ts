import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
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
    public readonly search = model.required<string>();
    public readonly provider = model.required<string>();
    public readonly status = model.required<string>();
    public readonly kind = model.required<string>();

    public readonly filtersApply = output();
    public readonly filtersReset = output();

    protected getControlTextValue(value: FdUiInputValue): string {
        return fdUiCoerceInputTextValue(value);
    }
}

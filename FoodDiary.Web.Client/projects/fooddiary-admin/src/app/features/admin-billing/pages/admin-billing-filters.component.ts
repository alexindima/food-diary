import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import type { AdminBillingTab } from '../api/admin-billing.service';

@Component({
    selector: 'fd-admin-billing-filters',
    imports: [FormsModule, FdUiButtonComponent, FdUiInputComponent],
    templateUrl: './admin-billing-filters.component.html',
    styleUrl: './admin-billing.component.scss',
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
}

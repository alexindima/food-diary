import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiIconComponent } from 'fd-ui-kit';

import type { ActiveFilterChip } from '../../../shared/ui/filters/active-filter-chip';

@Component({
    selector: 'fd-active-filters',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiIconComponent],
    templateUrl: './active-filters.html',
    styleUrl: './active-filters.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveFiltersComponent {
    public readonly chips = input.required<readonly ActiveFilterChip[]>();
    public readonly filterRemoved = output<string>();
    public readonly filtersReset = output();
}

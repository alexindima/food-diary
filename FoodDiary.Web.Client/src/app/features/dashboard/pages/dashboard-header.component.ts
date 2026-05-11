import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDatePickerButtonComponent } from 'fd-ui-kit/date-picker-button/fd-ui-date-picker-button.component';

import type { DashboardHeaderState } from './dashboard-view.types';

@Component({
    selector: 'fd-dashboard-header',
    imports: [TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiDatePickerButtonComponent],
    templateUrl: './dashboard-header.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardHeaderComponent {
    public readonly headerState = input.required<DashboardHeaderState>();
    public readonly selectedDate = input.required<Date>();
    public readonly isEditingLayout = input.required<boolean>();

    public readonly dateChange = output<Date | null>();
    public readonly appearanceOpen = output<void>();
    public readonly notificationSettingsOpen = output<void>();
    public readonly settingsOpen = output<void>();
}

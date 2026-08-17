import { CdkTrapFocus } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FieldTree, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import type { CycleSettingsFormModel } from '../../lib/cycle-tracking.facade';
import type { CycleReproductiveState, CycleTrackingGoal, CycleTrackingMode } from '../../models/cycle.data';

@Component({
    selector: 'fd-cycle-settings-drawer',
    imports: [
        CdkTrapFocus,
        TranslatePipe,
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
        FdUiInputComponent,
        FdUiSelectComponent,
    ],
    templateUrl: './cycle-settings-drawer.html',
    styleUrl: '../cycle-day-editor-drawer/cycle-day-editor-drawer.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleSettingsDrawerComponent {
    public readonly settingsForm = input.required<FieldTree<CycleSettingsFormModel>>();
    public readonly modeOptions = input.required<Array<FdUiSelectOption<CycleTrackingMode>>>();
    public readonly goalOptions = input.required<Array<FdUiSelectOption<CycleTrackingGoal>>>();
    public readonly reproductiveStateOptions = input.required<Array<FdUiSelectOption<CycleReproductiveState>>>();
    public readonly isSaving = input.required<boolean>();
    public readonly isDeleting = input.required<boolean>();
    public readonly closed = output();
    public readonly deleteRequested = output();
}

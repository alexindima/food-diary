import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, untracked } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiModalDirective } from 'fd-ui-kit/dialog/fd-ui-modal.directive';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

import { DEVELOPMENT_SCREENS } from '../../../../shared/development/development-tools';
import type { CycleReproductiveState, CycleTrackingGoal, CycleTrackingMode } from '../../../../shared/models/cycle.data';
import { cycleSettingsDevelopmentScreen } from '../../lib/cycle-development';
import type { CycleSettingsFormModel } from '../../lib/cycle-tracking.facade';

@Component({
    selector: 'fd-cycle-settings-drawer',
    imports: [
        FdUiModalDirective,
        TranslatePipe,
        FormField,
        FdUiFormDirective,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
        FdUiInputComponent,
        FdUiFormErrorComponent,
        FdUiSelectComponent,
    ],
    templateUrl: './cycle-settings-drawer.html',
    styleUrl: '../cycle-day-editor-drawer/cycle-day-editor-drawer.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleSettingsDrawerComponent {
    private readonly developmentScreens = inject(DEVELOPMENT_SCREENS, { optional: true });
    private readonly translate = inject(TranslateService);
    public readonly settingsForm = input.required<FieldTree<CycleSettingsFormModel>>();
    public readonly modeOptions = input.required<Array<FdUiSelectOption<CycleTrackingMode>>>();
    public readonly goalOptions = input.required<Array<FdUiSelectOption<CycleTrackingGoal>>>();
    public readonly reproductiveStateOptions = input.required<Array<FdUiSelectOption<CycleReproductiveState>>>();
    public readonly isSaving = input.required<boolean>();
    public readonly error = input<string | null>(null);
    public readonly isDeleting = input.required<boolean>();
    public readonly closed = output();
    public readonly deleteRequested = output();

    protected readonly isBusy = computed(() => this.isSaving() || this.isDeleting());

    public constructor() {
        if (this.developmentScreens !== null) {
            const screens = this.developmentScreens;
            effect(onCleanup => {
                const settings = this.settingsForm();
                const screen = untracked(() =>
                    cycleSettingsDevelopmentScreen(settings, {
                        busy: () => this.isBusy(),
                        close: () => {
                            this.close();
                        },
                        error: () => this.error(),
                        translate: key => String(this.translate.instant(key)),
                    }),
                );
                onCleanup(screens.attach(screen));
            });
        }
    }

    protected close(): void {
        if (!this.isBusy()) {
            this.closed.emit();
        }
    }

    protected numberError(fieldName: 'averageCycleLength' | 'averagePeriodLength' | 'lutealLength'): string | null {
        const field = this.settingsForm()[fieldName]();
        if (!field.invalid() || (!field.touched() && !field.dirty())) {
            return null;
        }
        switch (fieldName) {
            case 'averageCycleLength': {
                return 'CYCLE_TRACKING.AVG_LENGTH_ERROR';
            }
            case 'averagePeriodLength': {
                return 'CYCLE_TRACKING.PERIOD_LENGTH_ERROR';
            }
            case 'lutealLength': {
                return 'CYCLE_TRACKING.LUTEAL_LENGTH_ERROR';
            }
        }
    }
}
